using System.Collections;
using System.Collections.Generic;
using Infrastructure.EventSourcing;
using Registration.Contracts;

namespace Registration.Events {
    public class AvailableSeatsChanged: VersionedEvent {
        public IEnumerable<SeatQuantity> Seats { get; set; } 
    }
}