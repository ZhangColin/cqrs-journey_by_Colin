using System;
using System.Collections.Generic;
using Infrastructure.EventSourcing;
using Registration.Contracts;

namespace Registration.Events {
    public class SeatsReserved: VersionedEvent {
        public Guid ReservationId { get; set; }
        public IEnumerable<SeatQuantity> ReservationDetails { get; set; }
        public IEnumerable<SeatQuantity> AvailableSeatsChanged { get; set; } 
    }
}