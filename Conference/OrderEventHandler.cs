using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Infrastructure.Messaging.Handling;
using Registration.Contracts.Events;

namespace Conference {
    public class OrderEventHandler:
        IEventHandler<OrderPlaced>,
        IEventHandler<OrderRegistrantAssigned>,
        IEventHandler<OrderTotalsCalculated>,
        IEventHandler<OrderConfirmed>,
        IEventHandler<OrderExpired>,
        IEventHandler<SeatAssignmentsCreated>,
        IEventHandler<SeatAssigned>,
        IEventHandler<SeatAssignmentUpdated>,
        IEventHandler<SeatUnassigned> {

        private Func<ConferenceContext> _contextFactory;
        public OrderEventHandler(Func<ConferenceContext> contextFactory) {
            this._contextFactory = contextFactory;
        }

        public void Handle(OrderPlaced @event) {
            using(var context = this._contextFactory.Invoke()) {
                context.Orders.Add(new Order(@event.ConferenceId, @event.SourceId, @event.AccessCode));
                context.SaveChanges();
            }
        }

        public void Handle(OrderRegistrantAssigned @event) {
            this.ProcessOrder(order => order.Id == @event.SourceId, order => {
                order.RegistrantEmail = @event.Email;
                order.RegistrantName = @event.LastName + ", " + @event.FirstName;
            });
        }

        public void Handle(OrderTotalsCalculated @event) {
            if(!this.ProcessOrder(order=>order.Id == @event.SourceId, order=>order.TotalAmount=@event.Total)) {
                Trace.TraceError("Failed to locate the order with id {0} to apply calculated totals", @event.SourceId);
            }
        }

        public void Handle(OrderConfirmed @event) {
            if(!this.ProcessOrder(order=>order.Id==@event.SourceId, order=>order.Status=Order.OrderStatus.Paid)) {
                Trace.TraceError("Failed to locate the order with {0} to apply confirmed payment.", @event.SourceId);
            }
        }

        public void Handle(OrderExpired @event) {
            using(var context = this._contextFactory.Invoke()) {
                var order = context.Orders.FirstOrDefault(x => x.Id == @event.SourceId);
                if(order!=null) {
                    context.Orders.Remove(order);
                    context.SaveChanges();
                }
            }
        }

        public void Handle(SeatAssignmentsCreated @event) {
            if(!this.ProcessOrder(order=>order.Id==@event.OrderId, order=>order.AssignmentsId=@event.SourceId)) {
                Trace.TraceError(
                    "Failed to locate the order with {0} for the seat assignments being created with id {1}.",
                    @event.OrderId, @event.SourceId);
            }
        }

        public void Handle(SeatAssigned @event) {
            if(!this.ProcessOrder(order=>order.AssignmentsId==@event.SourceId, order => {
                var seat = order.Seats.FirstOrDefault(x => x.Position == @event.Position);
                if(seat!=null) {
                    seat.Attendee.FirstName = @event.Attendee.FirstName;
                    seat.Attendee.LastName = @event.Attendee.LastName;
                    seat.Attendee.Email = @event.Attendee.Email;
                }
                else {
                    order.Seats.Add(new OrderSeat(@event.SourceId, @event.Position, @event.SeatType) {
                        Attendee = new Attendee() {
                            FirstName = @event.Attendee.FirstName,
                            LastName = @event.Attendee.LastName,
                            Email = @event.Attendee.Email
                        }
                    });
                }
            })) {
                Trace.TraceError(
                    "Failed to locate the order with seat assignments id {0} for the seat assignment being assigned at position {1}.",
                    @event.SourceId, @event.Position);
            }
        }

        public void Handle(SeatAssignmentUpdated @event) {
            if(!this.ProcessOrder(order=>order.AssignmentsId==@event.SourceId, order => {
                var seat = order.Seats.FirstOrDefault(x => x.Position == @event.Position);
                if(seat!=null) {
                    seat.Attendee.FirstName = @event.Attendee.FirstName;
                    seat.Attendee.LastName = @event.Attendee.LastName;
                }
                else {
                    Trace.TraceError("Failed to locate the seat being updated at position {0} for assignment {1}.",
                        @event.Position, @event.SourceId);
                }
            })) {
                Trace.TraceError(
                    "Failed to locate the order with seat assignments id {0} for the seat assignment being updated at position {1}.",
                    @event.SourceId, @event.Position);
            }
        }

        public void Handle(SeatUnassigned @event) {
            if(!this.ProcessOrder(order=>order.AssignmentsId==@event.SourceId, order => {
                var seat = order.Seats.FirstOrDefault(x => x.Position == @event.Position);
                if (seat != null) {
                    order.Seats.Remove(seat);
                }
                else {
                    Trace.TraceError("Failed to locate the seat being unassigned at position {0} for assignment {1}.",
                        @event.Position, @event.SourceId);
                }
            })) {
                Trace.TraceError(
                    "Failed to locate the order with seat assignments id {0} for the seat being unassigned at position {1}.",
                    @event.SourceId, @event.Position);
            }
        }

        private bool ProcessOrder(Expression<Func<Order, bool>> lookup, Action<Order> orderAction) {
            using(var context = this._contextFactory.Invoke()) {
                var order = context.Orders.Include(o => o.Seats).FirstOrDefault(lookup);
                if(order!=null) {
                    orderAction.Invoke(order);
                    context.SaveChanges();
                    return true;
                }
                else {
                    return false;
                }
            }
        }
    }
}