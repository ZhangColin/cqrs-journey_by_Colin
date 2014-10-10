using System.Collections.Generic;
using System.IO;
using System.Linq;
using Infrastructure.Messaging;
using Infrastructure.Serialization;

namespace Infrastructure.Sql.Messaging {
    public class EventBus: IEventBus {
        private readonly IMessageSender _sender;
        private readonly ITextSerializer _serializer;

        public EventBus(IMessageSender sender, ITextSerializer serializer) {
            this._sender = sender;
            this._serializer = serializer;
        }

        public void Publish(Envelope<IEvent> @event) {
            _sender.Send(this.BuildMessage(@event));
        }

        public void Publish(IEnumerable<Envelope<IEvent>> events) {
            _sender.Send(events.Select(this.BuildMessage));
        }

        private Message BuildMessage(Envelope<IEvent> @event) {
            using(var payloadWriter = new StringWriter()) {
                _serializer.Serialize(payloadWriter, @event.Body);
                return new Message(payloadWriter.ToString(), correlationId:@event.CorrelationId);
            }
        }
    }
}