using System;

namespace Registration.Commands {
    public class CommitSeatReservation: SeatsAvailabilityCommand {
        public Guid ReservationId { get; set; }
    }
}