using System;

namespace Infrastructure.Sql.Messaging {
    public class MessageReceivedEventArgs: EventArgs {
        public Message Message { get; private set; }

        public MessageReceivedEventArgs(Message message) {
            this.Message = message;
        }
    }
}