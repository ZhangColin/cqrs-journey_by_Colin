using System;
using Infrastructure.EventSourcing;

namespace Registration.Contracts.Events {
    public class SeatAssigned: VersionedEvent {
        public SeatAssigned(Guid sourceId) {
            this.SourceId = sourceId;
        }

        public int Position { get; set; }
        public Guid SeatType { get; set; }
        public PersonalInfo Attendee { get; set; }
    }
}