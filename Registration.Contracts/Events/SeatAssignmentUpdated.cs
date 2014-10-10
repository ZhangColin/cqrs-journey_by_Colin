using System;
using Infrastructure.EventSourcing;

namespace Registration.Contracts.Events {
    public class SeatAssignmentUpdated: VersionedEvent {

        public SeatAssignmentUpdated(Guid sourceId) {
            this.SourceId = sourceId;
        }

        public int Position { get; set; }
        public PersonalInfo Attendee { get; set; }
    }
}