using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Infrastructure;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using NUnit.Framework;

namespace Registration.Tests {
    public class EventSourcingTestHelper<T> where T: IEventSourced {
        private ICommandHandler _handler;
        private readonly RepositoryStub _repository;
        private string _expectedCorrelationid;

        public List<IVersionedEvent> Events { get; private set; }

        public IEventSourcedRepository<T> Repository { get { return this._repository; } } 
        public EventSourcingTestHelper() {
            this.Events = new List<IVersionedEvent>();
            this._repository = new RepositoryStub((eventSourced, collelationId) => {
                if(this._expectedCorrelationid!=null) {
                    Assert.AreEqual(this._expectedCorrelationid, collelationId);
                }

                this.Events.AddRange(eventSourced.Events);
            });
        }

        public void Setup(ICommandHandler handler) {
            this._handler = handler;
        }

        public void Given(params IVersionedEvent[] history) {
            this._repository.History.AddRange(history);
        }

        public void When(ICommand command) {
            this._expectedCorrelationid = command.Id.ToString();
            ((dynamic)this._handler).Handle((dynamic)command);
            this._expectedCorrelationid = null;
        }

        public void When(IEvent @event) {
            ((dynamic)this._handler).Handle((dynamic)@event);
        }

        public bool ThenContains<TEvent>() where TEvent: IVersionedEvent {
            return this.Events.Any(x => x.GetType() == typeof(TEvent));
        }

        public TEvent ThenHasSingle<TEvent>() where TEvent: IVersionedEvent {
            Assert.AreEqual(1, this.Events.Count);
            var @event = this.Events.Single();
            Assert.IsAssignableFrom<TEvent>(@event);
            return (TEvent)@event;
        }

        public TEvent ThenHasOne<TEvent>() where TEvent: IVersionedEvent {
            Assert.AreEqual(1, this.Events.OfType<TEvent>().Count());
            var @event = this.Events.OfType<TEvent>().Single();
            return @event;
        }

        private class RepositoryStub: IEventSourcedRepository<T> {
            private readonly Action<T, string> _onSave;
            public readonly List<IVersionedEvent> History = new List<IVersionedEvent>();
            private readonly Func<Guid, IEnumerable<IVersionedEvent>, T> _entityFactory; 

            public RepositoryStub(Action<T, string> onSave ) {
                this._onSave = onSave;

                var constructor = typeof(T).GetConstructor(new[] {typeof(Guid), typeof(IEnumerable<IVersionedEvent>)});
                if(constructor==null) {
                    throw new InvalidCastException(
                        "Type T must have a constructor with the following signature: .ctor(Guid, IEnumerable<IVersionedEvent>)");
                }

                _entityFactory = (id, events) => (T)constructor.Invoke(new object[] {id, events});
            }

            T IEventSourcedRepository<T>.Find(Guid id) {
                var all = this.History.Where(x => x.SourceId == id).ToList();
                if(all.Count>0) {
                    return this._entityFactory.Invoke(id, all);
                }

                return default(T);
            }

            T IEventSourcedRepository<T>.Get(Guid id) {
                var entity = ((IEventSourcedRepository<T>)this).Find(id);

                if(Equals(entity, default(T))) {
                    throw new EntityNotFoundException(id, "Test");
                }

                return entity;
            }

            void IEventSourcedRepository<T>.Save(T eventSourced, string correlationId) {
                this._onSave(eventSourced, correlationId);
            }
        }
    }
}