using System;
using System.Diagnostics;
using System.IO;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Serialization;
using Infrastructure.Sql.Messaging;
using Infrastructure.Sql.Messaging.Handling;
using Moq;
using NUnit.Framework;

namespace Infrastructure.Sql.IntegrationTests.Messaging {
    [TestFixture]
    public class EventProcessorFixture {
        private Mock<IMessageReceiver> _receiverMock;
        private EventProcessor _processor;

        [SetUp]
        public void Setup() {
            Trace.Listeners.Clear();
            _receiverMock = new Mock<IMessageReceiver>();
            _processor = new EventProcessor(_receiverMock.Object, CreateSerializer());
        }

        [Test]
        public void WhenStartingThenStartsReceiver() {
            this._processor.Start();
            this._receiverMock.Verify(r=>r.Start());
        }

        [Test]
        public void WhenStoppingAfterStartingThenStopsReceiver() {
            this._processor.Start();
            this._processor.Stop();
            this._receiverMock.Verify(r=>r.Stop());
        }

        [Test]
        public void WhenReceivesMessageThenNotifiesRegisteredHandler() {
            var handlerAMock = new Mock<IEventHandler>();
            handlerAMock.As<IEventHandler<Event1>>();
            handlerAMock.As<IEventHandler<Event2>>();

            var handlerBMock = new Mock<IEventHandler>();
            handlerBMock.As<IEventHandler<Event2>>();

            this._processor.Register(handlerAMock.Object);
            this._processor.Register(handlerBMock.Object);

            this._processor.Start();

            var event1 = new Event1 {SourceId = Guid.NewGuid()};
            var event2 = new Event2 {SourceId = Guid.NewGuid()};

            this._receiverMock.Raise(r=>r.MessageReceived+=null, new MessageReceivedEventArgs(new Message(Serialize(event1))));
            this._receiverMock.Raise(r=>r.MessageReceived+=null, new MessageReceivedEventArgs(new Message(Serialize(event2))));

            handlerAMock.As<IEventHandler<Event1>>().Verify(h=>h.Handle(It.Is<Event1>(e=>e.SourceId==event1.SourceId)));
            handlerAMock.As<IEventHandler<Event2>>().Verify(h=>h.Handle(It.Is<Event2>(e=>e.SourceId==event2.SourceId)));
            handlerBMock.As<IEventHandler<Event2>>().Verify(h=>h.Handle(It.Is<Event2>(e=>e.SourceId==event2.SourceId)));
        }

        [Test]
        public void WhenReceivesMessageThenNotifiesGenericHandler() {
            var handler = new Mock<IEventHandler>();
            handler.As<IEventHandler<IEvent>>();

            this._processor.Register(handler.Object);

            this._processor.Start();

            var event1 = new Event1 {SourceId = Guid.NewGuid()};
            var event2 = new Event2 {SourceId = Guid.NewGuid()};

            this._receiverMock.Raise(r=>r.MessageReceived+=null, new MessageReceivedEventArgs(new Message(Serialize(event1))));
            this._receiverMock.Raise(r=>r.MessageReceived+=null, new MessageReceivedEventArgs(new Message(Serialize(event2))));

            handler.As<IEventHandler<IEvent>>().Verify(h=>h.Handle(It.Is<Event1>(e=>e.SourceId==event1.SourceId)));
            handler.As<IEventHandler<IEvent>>().Verify(h=>h.Handle(It.Is<Event2>(e=>e.SourceId==event2.SourceId)));
        }

        private static string Serialize(object payload) {
            var serializer = CreateSerializer();

            using (var writer = new StringWriter()) {
                serializer.Serialize(writer, payload);
                return writer.ToString();
            }
        }

        private static ITextSerializer CreateSerializer() {
            return new JsonTextSerializer();
        }

        public class Event1 : IEvent {
            public Guid SourceId { get; set; }
        }

        public class Event2 : IEvent {
            public Guid SourceId { get; set; }
        }
    }
}