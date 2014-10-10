using System.Collections.Generic;
using Infrastructure.EventSourcing;

namespace Registration.Contracts.Events {
    public class OrderUpdated: VersionedEvent {
        public IEnumerable<SeatQuantity> Seats { get; set; } 
    }
}