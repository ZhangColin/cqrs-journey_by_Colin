using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Serialization;

namespace Infrastructure.Sql.Messaging.Handling {
    public class EventProcessor: MessageProcessor, IEventHandlerRegistry {
        private EventDispatcher _messageDispatcher;

        public EventProcessor(IMessageReceiver receiver, ITextSerializer serializer):
            base(receiver, serializer) {
            _messageDispatcher = new EventDispatcher();
        }

        protected override void ProcessMessage(object payload, string correlationId) {
            var @event = (IEvent)payload;
            this._messageDispatcher.DispatchMessage(@event, null, correlationId, "");
        }

        public void Register(IEventHandler handler) {
            this._messageDispatcher.Register(handler);
        }
    }
}