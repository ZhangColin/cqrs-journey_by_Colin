using System;
using Infrastructure.EventSourcing;

namespace Registration.Contracts.Events {
    public class SeatUnassigned: VersionedEvent {
        public int Position { get; set; }

        public SeatUnassigned(Guid sourceId) {
            this.SourceId = sourceId;
        }
    }
}