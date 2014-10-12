using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using Infrastructure.Messaging;
using Registration.Commands;
using Registration.ReadModel;

namespace Conference.Web.Public.Controllers {
    public class OrderController: ConferenceTenantController {
        private readonly IOrderDao _orderDao;
        private readonly ICommandBus _bus;

        static OrderController() {
            Mapper.CreateMap<OrderSeat, AssignSeat>();
        }

        public OrderController(IConferenceDao conferenceDao, IOrderDao orderDao, ICommandBus bus): base(conferenceDao) {
            this._orderDao = orderDao;
            this._bus = bus;
        }

        [Display]
        public ActionResult Display(Guid orderId) {
            var order = _orderDao.FindPricedOrder(orderId);
            if(order == null) {
                return RedirectToAction("Find", new {conferenceCode = ConferenceCode});
            }

            return this.View(order);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public RedirectToRouteResult AssignSeatsForOrder(Guid orderId) {
            var order = _orderDao.FindPricedOrder(orderId);

            if(order == null || !order.AssignmentsId.HasValue) {
                return RedirectToAction("Display", new {orderId});
            }

            return RedirectToAction("AssignSeats", new {assignmentsId = order.AssignmentsId});
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult AssignSeats(Guid assignmentsId) {
            var assignments = this._orderDao.FindOrderSeats(assignmentsId);
            if(assignments == null) {
                return RedirectToAction("Find", new {conferenceCode = this.ConferenceCode});
            }

            return this.View(assignments);
        }

        [HttpPost]
        public ActionResult AssignSeats(Guid assignmentsId, List<OrderSeat> seats) {
            var saved = this._orderDao.FindOrderSeats(assignmentsId);
            if(saved==null) {
                return RedirectToAction("Find", new {conferenceCode = this.ConferenceCode});
            }

            var pairs = seats.Where(seat => seat != null)
                .Select(seat => new {Saved = saved.Seats.FirstOrDefault(x => x.Position == seat.Position), New = seat})
                .Where(pair => pair.Saved != null)
                .Where(pair => pair.Saved.Attendee.Email != null || pair.New.Attendee.Email != null)
                .ToList();

            var unassigned = pairs.Where(x => !string.IsNullOrWhiteSpace(x.Saved.Attendee.Email)
                && string.IsNullOrWhiteSpace(x.New.Attendee.Email))
                .Select(x => (ICommand)new UnassignSeat {
                    SeatAssignmentsId = saved.AssignmentsId, Position = x.Saved.Position
                });

            var changed = pairs.Where(x => x.Saved.Attendee != x.New.Attendee && x.New.Attendee.Email != null)
                .Select(x => (ICommand)Mapper.Map(x.New, new AssignSeat {
                    SeatAssignmentsId = saved.AssignmentsId
                }));

            var commands = unassigned.Union(changed).ToList();
            if(commands.Count>0) {
                this._bus.Send(commands);
            }

            return RedirectToAction("Display", new {orderId = saved.OrderId});
        }

        [HttpGet]
        public ActionResult Find() {
            return this.View();
        }

        [HttpPost]
        public ActionResult Find(string email, string accessCode) {
            var orderId = this._orderDao.LocateOrder(email, accessCode);

            if(!orderId.HasValue) {
                return RedirectToAction("Find", new {conferenceCode = this.ConferenceCode});
            }

            return RedirectToAction("Display", new {conferenceCode = this.ConferenceCode, orderId = orderId.Value});
        }
    }
}