using System;
using System.Collections.Generic;
using Infrastructure.Messaging;

namespace Infrastructure.Sql.IntegrationTests {
    public class TestEventPublishingAggregateRoot: TestAggregateRoot, IEventPublisher {
        private readonly List<IEvent> _events = new List<IEvent>(); 

        protected TestEventPublishingAggregateRoot() {}

        public TestEventPublishingAggregateRoot(Guid id): base(id) {}

        public void AddEvent(IEvent @event) {
            this._events.Add(@event);
        }

        public IEnumerable<IEvent> Events {
            get { return _events; }
        }
    }
}