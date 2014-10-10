using Infrastructure.Messaging;

namespace Infrastructure.EventSourcing {
    /// <summary>
    /// 具有版本的事件接口
    /// </summary>
    public interface IVersionedEvent: IEvent {
        int Version { get; }
    }
}