using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Sql.Utils;

namespace Infrastructure.Sql.EventSourcing {
    public class SqlEventSourcedRepository<T>: IEventSourcedRepository<T> where T: class, IEventSourced {
        private static readonly string SourceType = typeof(T).Name;
        private readonly IEventBus _eventBus;
        private readonly ITextSerializer _serializer;
        private readonly Func<EventStoreDbContext> _contextFactory;
        private readonly Func<Guid, IEnumerable<IVersionedEvent>, T> _entityFactory;


        public SqlEventSourcedRepository(IEventBus eventBus, ITextSerializer serializer, Func<EventStoreDbContext> contextFactory) {
            this._eventBus = eventBus;
            this._serializer = serializer;
            this._contextFactory = contextFactory;

            ConstructorInfo constructor =
                typeof(T).GetConstructor(new[] {typeof(Guid), typeof(IEnumerable<IVersionedEvent>)});

            if(constructor==null) {
                throw new InvalidCastException(
                    "Type T must have a contructor with the following signature: .ctor(Guid, IEnumerable<IVersionedEvent>)");
            }

            this._entityFactory = (id, events) => (T)constructor.Invoke(new object[] {id, events});
        }

        public T Find(Guid id) {
            using(EventStoreDbContext context = this._contextFactory.Invoke()) {
                var deserialized = context.Set<Event>()
                    .Where(x => x.AggregateId == id && x.AggregateType == SourceType)
                    .OrderBy(x => x.Version)
                    .AsEnumerable()
                    .Select(this.Deserialize)
                    .AsCachedAnyEnumerable();

                if(deserialized.Any()) {
                    return _entityFactory.Invoke(id, deserialized);
                }

                return null;
            }
        }

        public T Get(Guid id) {
            T entity = this.Find(id);
            if(entity==null) {
                throw new EntityNotFoundException(id, SourceType);
            }

            return entity;
        }

        public void Save(T eventSourced, string correlationId) {
            IVersionedEvent[] events = eventSourced.Events.ToArray();
            using(EventStoreDbContext context = this._contextFactory.Invoke()) {
                DbSet<Event> eventsSet = context.Set<Event>();
                foreach(IVersionedEvent e in events) {
                    eventsSet.Add(this.Serialize(e, correlationId));
                }

                context.SaveChanges();
            }

            this._eventBus.Publish(events.Select(e => new Envelope<IEvent>(e) {CorrelationId = correlationId}));
        }

        private Event Serialize(IVersionedEvent e, string correlationId) {
            Event serialized;
            using(var writer = new StringWriter()) {
                this._serializer.Serialize(writer, e);
                serialized = new Event() {
                    AggregateId = e.SourceId,
                    AggregateType = SourceType,
                    Version = e.Version,
                    Payload = writer.ToString(),
                    CorrelationId = correlationId
                };
            }

            return serialized;
        }

        private IVersionedEvent Deserialize(Event @event) {
            using(var reader = new StringReader(@event.Payload)) {
                return (IVersionedEvent)this._serializer.Deserialize(reader);
            }
        }
    }
}