using System.Collections.Generic;

namespace Infrastructure.Messaging {
    /// <summary>
    /// 事件总线
    /// </summary>
    public interface IEventBus {
        void Publish(Envelope<IEvent> @event);
        void Publish(IEnumerable<Envelope<IEvent>> events);
    }
}