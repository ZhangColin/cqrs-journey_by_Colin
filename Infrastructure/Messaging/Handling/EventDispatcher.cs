using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Infrastructure.Messaging.Handling {
    /// <summary>
    /// 事件高度器
    /// </summary>
    public class EventDispatcher {
        private Dictionary<Type, List<Tuple<Type, Action<Envelope>>>> _handlersByEventType;
        private Dictionary<Type, Action<IEvent, string, string, string>> _dispatchersByEventType;

        public EventDispatcher() {
            this._handlersByEventType = new Dictionary<Type, List<Tuple<Type, Action<Envelope>>>>();
            this._dispatchersByEventType = new Dictionary<Type, Action<IEvent, string, string, string>>();
        }

        public EventDispatcher(IEnumerable<IEventHandler> handlers)
            : this() {
            foreach(IEventHandler handler in handlers) {
                this.Register(handler);
            }
        }

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        /// <param name="handler"></param>
        public void Register(IEventHandler handler) {
            Type handlerType = handler.GetType();

            foreach(Tuple<Type, Action<Envelope>> invocationTuple in this.BuildHandlerInvocations(handler)) {
                Type envelopeType = typeof(Envelope<>).MakeGenericType(invocationTuple.Item1);

                List<Tuple<Type, Action<Envelope>>> invocations;
                if(!this._handlersByEventType.TryGetValue(invocationTuple.Item1, out  invocations)) {
                    invocations = new List<Tuple<Type, Action<Envelope>>>();
                    this._handlersByEventType[invocationTuple.Item1] = invocations;
                }
                invocations.Add(new Tuple<Type, Action<Envelope>>(handlerType, invocationTuple.Item2));

                if(!this._dispatchersByEventType.ContainsKey(invocationTuple.Item1)) {
                    this._dispatchersByEventType[invocationTuple.Item1] =
                        this.BuildDispatchInvocation(invocationTuple.Item1);
                }
            }
        }

        public void DispatchMessages(IEnumerable<IEvent> events) {
            foreach(IEvent @event in events) {
                this.DispatchMessage(@event);
            }
        }

        public void DispatchMessage(IEvent @event) {
            this.DispatchMessage(@event, null, null, "");
        }

        public void DispatchMessage(IEvent @event, string messageId, string correlationId, string traceIdentifier) {
            Action<IEvent, string, string, string> dispatch;
            if(this._dispatchersByEventType.TryGetValue(@event.GetType(), out dispatch)) {
                dispatch(@event, messageId, correlationId, traceIdentifier);
            }

            if(this._dispatchersByEventType.TryGetValue(typeof(IEvent), out dispatch)) {
                dispatch(@event, messageId, correlationId, traceIdentifier);
            }
        }

        private void DoDispatchMessage<T>(T @event, string messageId, string correlationId, string traceIdentifier) {
            Envelope<T> envelope = Envelope.Create(@event);
            envelope.MessageId = messageId;
            envelope.CorrelationId = correlationId;

            List<Tuple<Type, Action<Envelope>>> handlers;

            if(this._handlersByEventType.TryGetValue(typeof(T), out  handlers)) {
                foreach(Tuple<Type, Action<Envelope>> handler in handlers) {
                    handler.Item2(envelope);
                }
            }
        }

        /// <summary>
        /// 创建处理器调用
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        private IEnumerable<Tuple<Type, Action<Envelope>>> BuildHandlerInvocations(IEventHandler handler) {
            Type[] interfaces = handler.GetType().GetInterfaces();
            IEnumerable<Tuple<Type, Action<Envelope>>> eventHandlerInvocations =
                interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                    .Select(i => new {
                        HandlerInterface = i,
                        EventType = i.GetGenericArguments()[0]
                    })
                    .Select(e => new Tuple<Type, Action<Envelope>>(e.EventType,
                        this.BuildHandlerInvocation(handler, e.HandlerInterface, e.EventType)));

            IEnumerable<Tuple<Type, Action<Envelope>>> envelopedEventHandlerInvocations =
                interfaces.Where(
                    i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnvelopedEventHandler<>))
                    .Select(i => new {
                        HandlerInterface = i,
                        EventType = i.GetGenericArguments()[0]
                    })
                    .Select(
                        e =>
                            new Tuple<Type, Action<Envelope>>(e.EventType,
                                this.BuildEnvelopeHandlerInvocation(handler, e.HandlerInterface, e.EventType)));

            return eventHandlerInvocations.Union(envelopedEventHandlerInvocations);
        }

        private Action<Envelope> BuildHandlerInvocation(IEventHandler handler, Type handlerType, Type messageType) {
            Type envelopeType = typeof(Envelope<>).MakeGenericType(messageType);
            ParameterExpression parameter = Expression.Parameter(typeof(Envelope));
            LambdaExpression invocationExpression = Expression.Lambda(
                Expression.Block(Expression.Call(Expression.Convert(Expression.Constant(handler), handlerType),
                    handlerType.GetMethod("Handle"), 
                    Expression.Property(Expression.Convert(parameter, envelopeType), "Body"))), parameter);

            return (Action<Envelope>)invocationExpression.Compile();
        }

        private Action<Envelope> BuildEnvelopeHandlerInvocation(IEventHandler handler, Type handlerType, Type messageType) {
            Type envelopeType = typeof(Envelope<>).MakeGenericType(messageType);

            ParameterExpression parameter = Expression.Parameter(typeof(Envelope));
            LambdaExpression invocationExpression = Expression.Lambda(
                Expression.Block(Expression.Call(Expression.Convert(Expression.Constant(handler), handlerType),
                    handlerType.GetMethod("Handle"),
                    Expression.Convert(parameter, envelopeType))), parameter);

            return (Action<Envelope>)invocationExpression.Compile();
        }

        private Action<IEvent, string, string, string> BuildDispatchInvocation(Type eventType) {
            ParameterExpression eventParameter = Expression.Parameter(typeof(IEvent));
            ParameterExpression messageIdParameter = Expression.Parameter(typeof(string));
            ParameterExpression correlationIdParameter = Expression.Parameter(typeof(string));
            ParameterExpression traceIdParameter = Expression.Parameter(typeof(string));

            LambdaExpression dispatchExpression = Expression.Lambda(
                Expression.Block(Expression.Call(Expression.Constant(this),
                    this.GetType()
                        .GetMethod("DoDispatchMessage", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(eventType),
                    Expression.Convert(eventParameter, eventType),
                    messageIdParameter,
                    correlationIdParameter,
                    traceIdParameter)),
                eventParameter,
                messageIdParameter,
                correlationIdParameter,
                traceIdParameter);

            return (Action<IEvent, string, string, string>)dispatchExpression.Compile();
        }
    }
}