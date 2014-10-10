using System;
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
    public class CommandProcessorFixture {
        private Mock<IMessageReceiver> _receiverMock;
        private CommandProcessor _processor;

        [SetUp]
        public void Setup() {
            _receiverMock = new Mock<IMessageReceiver>();
            _processor = new CommandProcessor(_receiverMock.Object, new JsonTextSerializer());
        }

        private static string Serialize(object payload) {
            var serializer = new JsonTextSerializer();

            using(var writer = new StringWriter()) {
                serializer.Serialize(writer, payload);
                return writer.ToString();
            }
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
            var handlerAMock = new Mock<ICommandHandler>();
            handlerAMock.As<ICommandHandler<Command1>>();

            var handlerBMock = new Mock<ICommandHandler>();
            handlerBMock.As<ICommandHandler<Command2>>();

            this._processor.Register(handlerAMock.Object);
            this._processor.Register(handlerBMock.Object);

            this._processor.Start();

            var command1 = new Command1 {Id = Guid.NewGuid()};
            var command2 = new Command2 {Id = Guid.NewGuid()};

            this._receiverMock.Raise(r=>r.MessageReceived+=null, new MessageReceivedEventArgs(new Message(Serialize(command1))));
            this._receiverMock.Raise(r=>r.MessageReceived+=null, new MessageReceivedEventArgs(new Message(Serialize(command2))));

            handlerAMock.As<ICommandHandler<Command1>>().Verify(h=>h.Handle(It.Is<Command1>(e=>e.Id==command1.Id)));
            handlerBMock.As<ICommandHandler<Command2>>().Verify(h=>h.Handle(It.Is<Command2>(e=>e.Id==command2.Id)));
        }

        public class Command1 : ICommand {
            public Guid Id { get; set; }
        }

        public class Command2 : ICommand {
            public Guid Id { get; set; }
        }
    }
}