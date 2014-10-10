using System;
using System.Collections.Generic;
using Infrastructure.Messaging;

namespace Infrastructure.EventSourcing {
    /// <summary>
    /// 事件溯源实体基类
    /// </summary>
    public class EventSourced: IEventSourced {
        private readonly Dictionary<Type, Action<IVersionedEvent>> _handlers =
            new Dictionary<Type, Action<IVersionedEvent>>();

        private readonly List<IVersionedEvent> _pendingEvents = new List<IVersionedEvent>();

        private readonly Guid _id;
        private int _version = -1;

        public EventSourced(Guid id) {
            this._id = id;
        }

        public Guid Id {
            get { return this._id; }
        }

        public int Version {
            get { return this._version; }
            protected set { this._version = value; }
        }

        public IEnumerable<IVersionedEvent> Events {
            get { return this._pendingEvents; }
        }

        protected void Handles<TEvent>(Action<TEvent> handler)
            where TEvent : IEvent {
            this._handlers.Add(typeof(TEvent), @event=>handler((TEvent)@event));
        }

        protected void LoadFrom(IEnumerable<IVersionedEvent> pastEvents) {
            foreach(var e in pastEvents) {
                this._handlers[e.GetType()].Invoke(e);
                this._version = e.Version;
            }
        }

        protected void Update(VersionedEvent e) {
            e.SourceId = this.Id;
            e.Version = this._version + 1;
            this._handlers[e.GetType()].Invoke(e);
            this._version = e.Version;
            this._pendingEvents.Add(e);
        }
    }
}