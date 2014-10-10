using System.Linq;
using System.Reflection;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging.Handling;
using Registration.Commands;

namespace Registration.Handlers {
    public class OrderCommandHandler:
        ICommandHandler<RegisterToConference>,
        ICommandHandler<MarkSeatsAsReserved>,
        ICommandHandler<RejectOrder>,
        ICommandHandler<AssignRegistrantDetails>,
        ICommandHandler<ConfirmOrder> {
        private readonly IEventSourcedRepository<Order> _repository;
        private readonly IPricingService _pricingService;

        public OrderCommandHandler(IEventSourcedRepository<Order> repository, IPricingService pricingService) {
            this._repository = repository;
            this._pricingService = pricingService;
        }

        public void Handle(RegisterToConference command) {
            var items = command.Seats.Select(t => new OrderItem(t.SeatType, t.Quantity)).ToList();
            var order = _repository.Find(command.OrderId);

            if(order==null) {
                order = new Order(command.OrderId, command.ConferenceId, items, _pricingService);
            }
            else {
                order.UpdateSeats(items, _pricingService);
            }

            _repository.Save(order, command.Id.ToString());
        }

        public void Handle(MarkSeatsAsReserved command) {
            var order = _repository.Get(command.OrderId);
            order.MarkAsReserved(this._pricingService, command.Expiration, command.Seats);
            _repository.Save(order, command.Id.ToString());
        }

        public void Handle(RejectOrder command) {
            var order = _repository.Find(command.OrderId);
            if(order!=null) {
                order.Expire();
                _repository.Save(order, command.Id.ToString());
            }
        }

        public void Handle(AssignRegistrantDetails command) {
            var order = _repository.Get(command.OrderId);
            order.AssignRegistrant(command.FirstName, command.LastName, command.Email);

            _repository.Save(order, command.Id.ToString());
        }

        public void Handle(ConfirmOrder command) {
            var order = _repository.Get(command.OrderId);
            order.Confirm();

            _repository.Save(order, command.Id.ToString());
        }
    }
}