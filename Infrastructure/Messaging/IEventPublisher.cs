using System.Collections.Generic;

namespace Infrastructure.Messaging {
    /// <summary>
    /// 事件发布器
    /// </summary>
    public interface IEventPublisher {
        IEnumerable<IEvent> Events { get; } 
    }
}