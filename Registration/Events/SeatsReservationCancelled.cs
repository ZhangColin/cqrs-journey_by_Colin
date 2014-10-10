using System;
using System.Collections.Generic;
using Infrastructure.EventSourcing;
using Registration.Contracts;

namespace Registration.Events {
    public class SeatsReservationCancelled: VersionedEvent {
        public Guid ReservationId { get; set; }
        public IEnumerable<SeatQuantity> AvailableSeatsChanged { get; set; } 
    }
}