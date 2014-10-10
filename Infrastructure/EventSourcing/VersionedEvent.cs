using System;

namespace Infrastructure.EventSourcing {
    public class VersionedEvent: IVersionedEvent {
        public Guid SourceId { get; set; }
        public int Version { get; set; }
    }
}