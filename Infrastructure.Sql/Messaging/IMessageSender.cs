using System.Collections.Generic;

namespace Infrastructure.Sql.Messaging {
    public interface IMessageSender {
        void Send(Message message);
        void Send(IEnumerable<Message> messages);
    }
}