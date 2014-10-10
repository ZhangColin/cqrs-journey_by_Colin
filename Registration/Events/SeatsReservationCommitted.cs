using System;
using Infrastructure.EventSourcing;

namespace Registration.Events {
    public class SeatsReservationCommitted: VersionedEvent {
        public Guid ReservationId { get; set; }
    }
}