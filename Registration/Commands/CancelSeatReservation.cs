﻿using System;

namespace Registration.Commands {
    public class CancelSeatReservation: SeatsAvailabilityCommand {
        public Guid ReservationId { get; set; }
    }
}