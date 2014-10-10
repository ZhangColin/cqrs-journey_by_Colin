using System;

namespace Infrastructure.Sql.Messaging {
    public interface IMessageReceiver {
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        void Start();

        void Stop();
    }
}