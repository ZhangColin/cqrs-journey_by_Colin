using System;
using Infrastructure.Messaging;

namespace Infrastructure.Tests.Messaging.Handling {
    public class EventA : IEvent {
        public Guid SourceId {
            get { return Guid.Empty; }
        }
    }
    
    public class EventB : IEvent {
        public Guid SourceId {
            get { return Guid.Empty; }
        }
    }
    
    public class EventC : IEvent {
        public Guid SourceId {
            get { return Guid.Empty; }
        }
    }
}