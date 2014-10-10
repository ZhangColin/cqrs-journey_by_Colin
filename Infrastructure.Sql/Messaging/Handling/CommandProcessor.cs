using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Serialization;

namespace Infrastructure.Sql.Messaging.Handling {
    public class CommandProcessor: MessageProcessor, ICommandHandlerRegistry {
        private Dictionary<Type, ICommandHandler> _handlers = new Dictionary<Type, ICommandHandler>(); 

        public CommandProcessor(IMessageReceiver receiver, ITextSerializer serializer)
            : base(receiver, serializer) {}

        protected override void ProcessMessage(object payload, string correlationId) {
            Type commandType = payload.GetType();
            ICommandHandler handler = null;

            if(this._handlers.TryGetValue(commandType, out handler)) {
                Trace.WriteLine("-- Handled by " + handler.GetType().FullName);
                ((dynamic)handler).Handle((dynamic)payload);
            }

            if(this._handlers.TryGetValue(typeof(ICommand), out handler)) {
                Trace.WriteLine("-- Handled by " + handler.GetType().FullName);
                ((dynamic)handler).Handle((dynamic)payload);
            }
        }

        public void Register(ICommandHandler handler) {
            Type genericHandler = typeof(ICommandHandler<>);
            List<Type> supportedCommandTypes = handler.GetType().GetInterfaces()
                .Where(iface=>iface.IsGenericType && iface.GetGenericTypeDefinition()==genericHandler)
                .Select(iface=>iface.GetGenericArguments()[0]).ToList();

            if(_handlers.Keys.Any(supportedCommandTypes.Contains)) {
                throw new ArgumentException("The command handled by the received handler already has a registered handler.");
            }

            foreach(Type commandType in supportedCommandTypes) {
                this._handlers.Add(commandType, handler);
            }
        }
    }
}