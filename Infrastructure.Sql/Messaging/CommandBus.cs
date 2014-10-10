using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Infrastructure.Messaging;
using Infrastructure.Serialization;

namespace Infrastructure.Sql.Messaging {
    public class CommandBus: ICommandBus {
        private readonly IMessageSender _sender;
        private readonly ITextSerializer _serializer;

        public CommandBus(IMessageSender sender, ITextSerializer serializer) {
            this._sender = sender;
            this._serializer = serializer;
        }

        public void Send(Envelope<ICommand> command) {
            this._sender.Send(this.BuildMessage(command));
        }

        public void Send(IEnumerable<Envelope<ICommand>> commands) {
            _sender.Send(commands.Select(this.BuildMessage));
        }

        private Message BuildMessage(Envelope<ICommand> command) {
            using(var payloadWriter = new StringWriter()) {
                _serializer.Serialize(payloadWriter, command.Body);
                return new Message(payloadWriter.ToString(),
                    command.Delay != TimeSpan.Zero ? (DateTime?)DateTime.UtcNow.Add(command.Delay) : null,
                    command.CorrelationId);
            }
        }
    }
}