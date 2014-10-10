using System;
using System.Collections.Generic;

namespace Infrastructure.EventSourcing {
    /// <summary>
    /// 代表一个事件溯源的实体
    /// </summary>
    public interface IEventSourced {
        Guid Id { get; }

        int Version { get; }

        IEnumerable<IVersionedEvent> Events { get; }
    }
}