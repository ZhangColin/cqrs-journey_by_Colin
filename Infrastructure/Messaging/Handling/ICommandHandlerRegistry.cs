namespace Infrastructure.Messaging.Handling {
    /// <summary>
    /// 命令处理器注册
    /// </summary>
    public interface ICommandHandlerRegistry {
        void Register(ICommandHandler handler);
    }
}