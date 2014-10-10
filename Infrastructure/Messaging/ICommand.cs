using System;

namespace Infrastructure.Messaging {
    /// <summary>
    /// 代表一个命令消息
    /// </summary>
    public interface ICommand {
        Guid Id { get; } 
    }
}