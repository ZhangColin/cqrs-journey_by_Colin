using System;
using System.Collections.Generic;
using Registration.Contracts;

namespace Registration.Commands {
    public class MakeSeatReservation: SeatsAvailabilityCommand {
        public MakeSeatReservation() {
            this.Seats = new List<SeatQuantity>();
        }

        public Guid ReservationId { get; set; }
        public List<SeatQuantity> Seats { get; set; }
    }
}