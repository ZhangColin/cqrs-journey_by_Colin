using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Messaging {
    /// <summary>
    /// 命令总线扩展，发送的命令使用消息信封包装
    /// </summary>
    public static class CommandBusExtensions {
        public static void Send(this ICommandBus bus, ICommand command) {
            bus.Send(new Envelope<ICommand>(command));
        }

        public static void Send(this ICommandBus bus, IEnumerable<ICommand> commands) {
            bus.Send(commands.Select(x=>new Envelope<ICommand>(x)));
        }
    }
}