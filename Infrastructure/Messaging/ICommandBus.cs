using System.Collections.Generic;

namespace Infrastructure.Messaging {
    /// <summary>
    /// 命令总线
    /// </summary>
    public interface ICommandBus {
        void Send(Envelope<ICommand> command);
        void Send(IEnumerable<Envelope<ICommand>> commands);
    }
}