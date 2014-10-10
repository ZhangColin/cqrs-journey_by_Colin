using System;
using System.Collections.Generic;
using Infrastructure.Messaging;

namespace Infrastructure.Processes {
    /// <summary>
    /// 长时任务管理器
    /// </summary>
    public interface IProcessManager {
        Guid Id { get; }
        bool Completed { get; }
        IEnumerable<Envelope<ICommand>> Commands { get; } 
    }
}