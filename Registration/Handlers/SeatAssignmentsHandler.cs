using AutoMapper;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging.Handling;
using Registration.Commands;
using Registration.Contracts.Events;

namespace Registration.Handlers {
    public class SeatAssignmentsHandler:
        IEventHandler<OrderConfirmed>,
        IEventHandler<OrderPaymentConfirmed>,
        ICommandHandler<UnassignSeat>,
        ICommandHandler<AssignSeat> {
        private readonly IEventSourcedRepository<Order> _orderRepository;
        private readonly IEventSourcedRepository<SeatAssignments> _assignmentsRepository;

        static SeatAssignmentsHandler() {
            Mapper.CreateMap<OrderPaymentConfirmed, OrderConfirmed>();
        }

        public SeatAssignmentsHandler(IEventSourcedRepository<Order> orderRepository,
            IEventSourcedRepository<SeatAssignments> assignmentsRepository) {
            this._orderRepository = orderRepository;
            this._assignmentsRepository = assignmentsRepository;
        }

        public void Handle(OrderConfirmed @event) {
            var order = this._orderRepository.Get(@event.SourceId);
            var assignments = order.CreateSeatAssignments();
            _assignmentsRepository.Save(assignments, null);
        }

        public void Handle(OrderPaymentConfirmed @event) {
            this.Handle(Mapper.Map<OrderConfirmed>(@event));
        }

        public void Handle(UnassignSeat command) {
            var assignments = this._assignmentsRepository.Get(command.SeatAssignmentsId);
            assignments.Unassign(command.Position);
            _assignmentsRepository.Save(assignments, command.Id.ToString());
        }

        public void Handle(AssignSeat command) {
            var assignments = this._assignmentsRepository.Get(command.SeatAssignmentsId);
            assignments.AssignSeat(command.Position, command.Attendee);
            _assignmentsRepository.Save(assignments, command.Id.ToString());
        }
    }
}