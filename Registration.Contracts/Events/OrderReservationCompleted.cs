using System;
using System.Collections.Generic;
using Infrastructure.EventSourcing;

namespace Registration.Contracts.Events {
    public class OrderReservationCompleted: VersionedEvent {
        public DateTime ReservationExpiration { get; set; }
        public IEnumerable<SeatQuantity> Seats { get; set; } 
    }
}