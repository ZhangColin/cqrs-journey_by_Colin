using System;

namespace Infrastructure.Messaging {
    /// <summary>
    /// 代表一个事件消息
    /// </summary>
    public interface IEvent {
        Guid SourceId { get; } 
    }
}