namespace Infrastructure.Messaging.Handling {
    /// <summary>
    /// 命令处理器接口
    /// </summary>
    public interface ICommandHandler {
         
    }

    public interface ICommandHandler<T>: ICommandHandler where T: ICommand {
        void Handle(T command);
    }
}