using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Messaging {
    /// <summary>
    /// 事件总线扩展，发布的事件使用消息信封包装
    /// </summary>
    public static class EventBusExtensions {
        public static void Publish(this IEventBus bus, IEvent @event) {
            bus.Publish(new Envelope<IEvent>(@event));
        }

        public static void Publish(this IEventBus bus, IEnumerable<IEvent> events) {
            bus.Publish(events.Select(x=>new Envelope<IEvent>(x)));
        }
    }
}