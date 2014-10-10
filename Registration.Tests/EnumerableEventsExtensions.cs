using System.Linq;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;

namespace Registration.Tests {
    public static class EnumerableEventsExtensions {
        public static TEvent SingleEvent<TEvent>(this IEventSourced aggregate) where TEvent: IEvent {
            return (TEvent)aggregate.Events.Single();
        }
    }
}