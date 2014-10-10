﻿using System;

namespace Registration.Commands {
    public class AddSeats: SeatsAvailabilityCommand {
        public Guid SeatType { get; set; }
        public int Quantity { get; set; }
    }
}