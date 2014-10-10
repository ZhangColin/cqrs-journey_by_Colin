using Infrastructure.EventSourcing;
using Infrastructure.Messaging.Handling;
using Registration.Commands;

namespace Registration.Handlers {
    public class SeatsAvailabilityHandler:
    ICommandHandler<MakeSeatReservation>,
    ICommandHandler<CancelSeatReservation>,
    ICommandHandler<CommitSeatReservation>,
    ICommandHandler<AddSeats>,
    ICommandHandler<RemoveSeats>
    {
        private readonly IEventSourcedRepository<SeatsAvailability> _repository;

        public SeatsAvailabilityHandler(IEventSourcedRepository<SeatsAvailability> repository) {
            this._repository = repository;
        }

        public void Handle(MakeSeatReservation command) {
            var availability = this._repository.Get(command.ConferenceId);
            availability.MakeReservation(command.ReservationId, command.Seats);
            this._repository.Save(availability, command.Id.ToString());
        }

        public void Handle(CancelSeatReservation command) {
            var availability = this._repository.Get(command.ConferenceId);
            availability.CancelReservation(command.ReservationId);
            this._repository.Save(availability, command.Id.ToString());
        }

        public void Handle(CommitSeatReservation command) {
            var availability = this._repository.Get(command.ConferenceId);
            availability.CommitReservation(command.ReservationId);
            this._repository.Save(availability, command.Id.ToString());
        }

        public void Handle(AddSeats command) {
            var availability = this._repository.Find(command.ConferenceId);
            if(availability==null) {
                availability = new SeatsAvailability(command.ConferenceId);
            }

            availability.AddSeats(command.SeatType, command.Quantity);
            this._repository.Save(availability, command.Id.ToString());
        }

        public void Handle(RemoveSeats command) {
            var availability = this._repository.Find(command.ConferenceId);
            if (availability == null) {
                availability = new SeatsAvailability(command.ConferenceId);
            }

            availability.RemoveSeats(command.SeatType, command.Quantity);
            this._repository.Save(availability, command.Id.ToString());
        }
    }
}