namespace Infrastructure.Messaging.Handling {
    /// <summary>
    /// 事件处理器注册
    /// </summary>
    public interface IEventHandlerRegistry {
        void Register(IEventHandler handler);
    }
}