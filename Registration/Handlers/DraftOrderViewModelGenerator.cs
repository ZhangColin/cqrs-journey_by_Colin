using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using AutoMapper;
using Conference.Common;
using Infrastructure.Messaging.Handling;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Registration.Handlers {
    public class DraftOrderViewModelGenerator:
        IEventHandler<OrderPlaced>,
        IEventHandler<OrderUpdated>,
        IEventHandler<OrderPartiallyReserved>,
        IEventHandler<OrderReservationCompleted>,
        IEventHandler<OrderRegistrantAssigned>,
        IEventHandler<OrderConfirmed>,
        IEventHandler<OrderPaymentConfirmed> {
        private readonly Func<ConferenceRegistrationDbContext> _contextFactory;

        static DraftOrderViewModelGenerator() {
            Mapper.CreateMap<OrderPaymentConfirmed, OrderConfirmed>();
        }

        public DraftOrderViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory) {
            this._contextFactory = contextFactory;
        }

        public void Handle(OrderPlaced @event) {
            using(var context  = _contextFactory.Invoke()) {
                var dto = new DraftOrder(@event.SourceId, @event.ConferenceId, DraftOrder.States.PendingReservation,
                    @event.Version) {
                        AccessCode = @event.AccessCode
                    };

                dto.Lines.AddRange(@event.Seats.Select(seat => new DraftOrderItem(seat.SeatType, seat.Quantity)));

                context.Save(dto);
            }
        }

        public void Handle(OrderUpdated @event) {
            using(var context = this._contextFactory.Invoke()) {
                var dto = context.Set<DraftOrder>().Include(o => o.Lines).First(o => o.OrderId == @event.SourceId);
                if(WasNotAlreadyHandled(dto, @event.Version)) {
                    var linesSet = context.Set<DraftOrderItem>();
                    foreach(var line in dto.Lines.ToArray()) {
                        linesSet.Remove(line);
                    }

                    dto.Lines.AddRange(@event.Seats.Select(seat => new DraftOrderItem(seat.SeatType, seat.Quantity)));

                    dto.State = DraftOrder.States.PendingReservation;
                    dto.OrderVersion = @event.Version;

                    context.Save(dto);
                }
            }
        }

        public void Handle(OrderPartiallyReserved @event) {
            this.UpdateReserved(@event.SourceId, @event.ReservationExpiration, DraftOrder.States.PartiallyReserved,
                @event.Version, @event.Seats);
        }

        public void Handle(OrderReservationCompleted @event) {
            this.UpdateReserved(@event.SourceId, @event.ReservationExpiration, DraftOrder.States.ReservationCompleted,
                @event.Version, @event.Seats);
        }

        public void Handle(OrderRegistrantAssigned @event) {
            using (var context = this._contextFactory.Invoke()) {
                var dto = context.Find<DraftOrder>(@event.SourceId);
                if (WasNotAlreadyHandled(dto, @event.Version)) {
                    dto.RegistrantEmail = @event.Email;
                    dto.OrderVersion = @event.Version;
                    context.Save(dto);
                }
            }
        }

        public void Handle(OrderConfirmed @event) {
            using(var context = this._contextFactory.Invoke()) {
                var dto = context.Find<DraftOrder>(@event.SourceId);
                if(WasNotAlreadyHandled(dto, @event.Version)) {
                    dto.State = DraftOrder.States.Confirmed;
                    dto.OrderVersion = @event.Version;

                    context.Save(dto);
                }
            }
        }

        public void Handle(OrderPaymentConfirmed @event) {
            this.Handle(Mapper.Map<OrderConfirmed>(@event));
        }

        private void UpdateReserved(Guid orderId, DateTime reservationExpiration, DraftOrder.States state,
            int orderVersion, IEnumerable<SeatQuantity> seats) {
            using(var context = this._contextFactory.Invoke()) {
                var dto = context.Set<DraftOrder>().Include(o => o.Lines).First(x => x.OrderId == orderId);
                if(WasNotAlreadyHandled(dto, orderVersion)) {
                    foreach(var seat in seats) {
                        var item = dto.Lines.Single(x => x.SeatType == seat.SeatType);
                        item.ReservedSeats = seat.Quantity;
                    }

                    dto.State = state;
                    dto.ReservationExpirationDate = reservationExpiration;

                    dto.OrderVersion = orderVersion;

                    context.Save(dto);
                }
            }
        }

        private static bool WasNotAlreadyHandled(DraftOrder draftOrder, int eventVersion) {
            if(eventVersion>draftOrder.OrderVersion) {
                return true;
            }
            else if(eventVersion==draftOrder.OrderVersion) {
                Trace.TraceWarning("Ignoring duplicate draft order update message with version {1} for order id {0}",
                    draftOrder.OrderId, eventVersion);
                return false;
            }
            else {
                Trace.TraceWarning(
                    @"An older order update message was received with with version {1} for order id {0}, last known version {2}.
This read model generator has an expectation that the EventBus will deliver messages for the same source in order.",
                    draftOrder.OrderId,
                    eventVersion,
                    draftOrder.OrderVersion);
                return false;
            }
        }
    }
}