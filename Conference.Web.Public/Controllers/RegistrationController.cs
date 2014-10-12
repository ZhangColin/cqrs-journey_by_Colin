using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Conference.Web.Public.Models;
using Conference.Web.Public.Utils;
using Infrastructure;
using Infrastructure.Messaging;
using Infrastructure.Utils;
using Payments.Contracts.Commands;
using Registration.Commands;
using Registration.ReadModel;

namespace Conference.Web.Public.Controllers {
    public class RegistrationController: ConferenceTenantController {
        public const string ThirdPartyProcessorPayment = "thirdParty";
        public const string InvoicePayment = "invoice";

        private static readonly TimeSpan DraftOrderWaitTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DraftOrderPollInterval = TimeSpan.FromMilliseconds(750);
        private static readonly TimeSpan PricedOrderWaitTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan PricedOrderPollInterval = TimeSpan.FromMilliseconds(750);
        private readonly ICommandBus _commandBus;
        private readonly IOrderDao _orderDao;

        public RegistrationController(ICommandBus commandBus, IOrderDao orderDao, IConferenceDao conferenceDao)
            : base(conferenceDao) {
            this._commandBus = commandBus;
            this._orderDao = orderDao;
        }

        [HttpGet]
        public Task<ActionResult> StartRegistration(Guid? orderId = null) {
            var viewModelTask = Task.Factory.StartNew(() => this.CreateViewModel());
            if(!orderId.HasValue) {
                return viewModelTask.ContinueWith<ActionResult>(t => {
                    var viewModel = t.Result;
                    viewModel.OrderId = GuidUtil.NewSequentialId();
                    return this.View(viewModel);
                });
            }
            else {
                return Task.Factory.ContinueWhenAll<ActionResult>(
                    new Task[] {
                        viewModelTask,
                        this.WaitUntilSeatsAreConfirmed(orderId.Value, 0)
                    }, tasks => {
                        var viewModel = ((Task<OrderViewModel>)tasks[0]).Result;
                        var order = ((Task<DraftOrder>)tasks[1]).Result;

                        if(order == null) {
                            return this.View("PricedOrderUnknown");
                        }

                        if(order.State == DraftOrder.States.Confirmed) {
                            return this.View("ShowCompletedOrder");
                        }

                        if(order.ReservationExpirationDate.HasValue && order.ReservationExpirationDate < DateTime.Now) {
                            return RedirectToAction("ShowExpiredOrder", new {
                                conferenceCode = this.ConferenceAlias.Code, orderId
                            });
                        }

                        UpdateViewModel(viewModel, order);

                        return this.View(viewModel);
                    });
            }
        }

        [HttpPost]
        public ActionResult StartRegistration(RegisterToConference command, int orderVersion) {
            var existingOrder = orderVersion != 0 ? this._orderDao.FindDraftOrder(command.OrderId) : null;
            var viewModel = this.CreateViewModel();

            if(existingOrder!=null) {
                this.UpdateViewModel(viewModel, existingOrder);
            }

            viewModel.OrderId = command.OrderId;

            if(!ModelState.IsValid) {
                return this.View(viewModel);
            }

            ModelState.Clear();
            bool needsExtraValidation = false;
            foreach(var seat in command.Seats) {
                var modelItem = viewModel.Items.FirstOrDefault(x => x.SeatType.Id == seat.SeatType);
                if(modelItem != null) {
                    if(seat.Quantity>modelItem.MaxSelectionQuantity) {
                        modelItem.PartiallyFulfilled = needsExtraValidation = true;
                        modelItem.OrderItem.RequestedSeats = modelItem.MaxSelectionQuantity;
                    }
                    else {
                        needsExtraValidation = false;
                    }
                }
            }

            if(needsExtraValidation) {
                return this.View(viewModel);
            }

            command.ConferenceId = this.ConferenceAlias.Id;
            this._commandBus.Send(command);

            return RedirectToAction("SpecifyRegistrantAndPaymentDetails", new {
                conferenceCode = this.ConferenceCode, orderId = command.OrderId, orderVersion
            });
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public Task<ActionResult> SpecifyRegistrantAndPaymentDetails(Guid orderId, int orderVersion) {
            return this.WaitUntilOrderIsPriced(orderId, orderVersion).ContinueWith<ActionResult>(t => {
                var pricedOrder = t.Result;
                if(pricedOrder == null) {
                    return this.View("PricedOrderUnknown");
                }

                if(!pricedOrder.ReservationExpirationDate.HasValue) {
                    return this.View("ShowCompletedOrder");
                }

                if(pricedOrder.ReservationExpirationDate < DateTime.UtcNow) {
                    return RedirectToAction("ShowExpiredOrder", new {
                        conferenceCode = this.ConferenceAlias.Code,
                        orderId = orderId
                    });
                }

                return this.View(new RegistrationViewModel {
                    RegistrantDetails = new AssignRegistrantDetails() {OrderId = orderId},
                    Order = pricedOrder
                });
            });
        }

        [HttpPost]
        public Task<ActionResult> SpecifyRegistrantAndPaymentDetails(AssignRegistrantDetails command, string paymentType,
            int orderVersion) {
            var orderId = command.OrderId;
            if(!ModelState.IsValid) {
                return SpecifyRegistrantAndPaymentDetails(orderId, orderVersion);
            }

            this._commandBus.Send(command);

            return this.StartPayment(orderId, paymentType, orderVersion);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult ShowExpiredOrder(Guid orderId) {
            return this.View();
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult ThankYou(Guid orderId) {
            var order = this._orderDao.FindDraftOrder(orderId);

            return this.View(order);
        }

        [HttpPost]
        public Task<ActionResult> StartPayment(Guid orderId, string paymentType, int orderVersion) {
            return this.WaitUntilSeatsAreConfirmed(orderId, orderVersion).ContinueWith(t => {
                var order = t.Result;
                if(order == null) {
                    return this.View("ReservationUnknown");
                }

                if(order.State == DraftOrder.States.PartiallyReserved) {
                    return RedirectToAction("StartRegistration", new {
                        conferenceCode = this.ConferenceCode,
                        orderId,
                        orderVersion = order.OrderVersion
                    });
                }

                if(order.State == DraftOrder.States.Confirmed) {
                    return this.View("ShowCompletedOrder");
                }

                if(order.ReservationExpirationDate.HasValue && order.ReservationExpirationDate < DateTime.UtcNow) {
                    return RedirectToAction("ShowExpiredOrder", new {
                        conferenceCode = this.ConferenceAlias.Code,
                        orderId = orderId
                    });
                }

                var pricedOrder = this._orderDao.FindPricedOrder(orderId);
                if(pricedOrder.IsFreeOfCharge) {
                    return CompleteRegistrationWithoutPayment(orderId);
                }

                switch(paymentType) {
                    case ThirdPartyProcessorPayment:
                        return CompleteRegistrationWithThirdPartyProcessorPayment(pricedOrder, orderVersion);
                    case InvoicePayment:
                        break;
                    default:
                        break;
                }

                throw new InvalidOperationException();
            });
        }

        private ActionResult CompleteRegistrationWithoutPayment(Guid orderId) {
            var confirmationCommand = new ConfirmOrder {OrderId = orderId};
            this._commandBus.Send(confirmationCommand);

            return RedirectToAction("ThankYou", new {conferenceCode = this.ConferenceAlias.Code, orderId});
        }

        private ActionResult CompleteRegistrationWithThirdPartyProcessorPayment(PricedOrder order, int orderVersion) {
            var paymentCommand = CreatePaymentCommand(order);
            this._commandBus.Send(paymentCommand);

            var paymentAcceptedUrl = this.Url.Action("ThankYou", new {
                conferenceCode = this.ConferenceAlias.Code, order.OrderId
            });
            var paymentRejectedUrl = this.Url.Action("SpecifyRegistrantAndPaymentDetails", new {
                conferenceCode = this.ConferenceAlias.Code,
                order.OrderId,
                orderVersion
            });

            return RedirectToAction("ThirdPartyProcessorPayment", "Payment", new {
                conferenceCode = this.ConferenceAlias.Code,
                paymentId = paymentCommand.PaymentId,
                paymentAcceptedUrl,
                paymentRejectedUrl
            });
        }

        private InitiateThirdPartyProcessorPayment CreatePaymentCommand(PricedOrder order) {
            var description = "Registration for " + this.ConferenceAlias.Name;
            var totalAmount = order.Total;

            var paymentCommand = new InitiateThirdPartyProcessorPayment() {
                PaymentId = GuidUtil.NewSequentialId(),
                ConferenceId = this.ConferenceAlias.Id,
                PaymentSourceId = order.OrderId,
                Description = description,
                TotalAmount = totalAmount
            };

            return paymentCommand;
        }

        private Task<DraftOrder> WaitUntilSeatsAreConfirmed(Guid orderId, int lastOrderVersion) {
            return TimerTaskFactory.StartNew(
                () => this._orderDao.FindDraftOrder(orderId),
                order => order != null && order.State != DraftOrder.States.PendingReservation
                    && order.OrderVersion > lastOrderVersion,
                DraftOrderPollInterval,
                DraftOrderWaitTimeout);
        }

        private void UpdateViewModel(OrderViewModel viewModel, DraftOrder order) {
            viewModel.OrderId = order.OrderId;
            viewModel.OrderVersion = order.OrderVersion;
            viewModel.ReservationExpirationDate = order.ReservationExpirationDate.ToEpochMilliseconds();

            foreach(var line in order.Lines) {
                var seat = viewModel.Items.First(s => s.SeatType.Id == line.SeatType);
                seat.OrderItem = line;
                seat.AvailableQuantityForOrder = seat.AvailableQuantityForOrder + line.RequestedSeats;
                seat.MaxSelectionQuantity = Math.Min(seat.AvailableQuantityForOrder, 20);
                seat.PartiallyFulfilled = line.RequestedSeats > line.ReservedSeats;
            }
        }

        private OrderViewModel CreateViewModel() {
            var seatTypes = this.ConferenceDao.GetPublishedSeatTypes(this.ConferenceAlias.Id);
            var viewModel = new OrderViewModel() {
                ConferenceId = this.ConferenceAlias.Id,
                ConferenceCode = this.ConferenceAlias.Code,
                ConferenceName = this.ConferenceAlias.Name,
                Items = seatTypes.Select(s => new OrderItemViewModel() {
                    SeatType = s,
                    OrderItem = new DraftOrderItem(s.Id, 0),
                    AvailableQuantityForOrder = Math.Max(s.AvailableQuantity, 0),
                    MaxSelectionQuantity = Math.Max(Math.Min(s.AvailableQuantity, 20), 0)
                }).ToList()
            };

            return viewModel;
        }

        private Task<PricedOrder> WaitUntilOrderIsPriced(Guid orderId, int lastOrderVersion) {
            return TimerTaskFactory.StartNew(
                () => this._orderDao.FindPricedOrder(orderId),
                order => order != null && order.OrderVersion > lastOrderVersion,
                PricedOrderPollInterval,
                PricedOrderWaitTimeout);
        } 
    }
}