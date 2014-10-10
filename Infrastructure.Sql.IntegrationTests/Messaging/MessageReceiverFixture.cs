using System;
using System.Data.Entity.Infrastructure;
using System.Threading;
using Infrastructure.Sql.Messaging;
using Infrastructure.Sql.Messaging.Implementation;
using NUnit.Framework;

namespace Infrastructure.Sql.IntegrationTests.Messaging {
    [TestFixture]
    public class MessageReceiverFixture {
        private IDbConnectionFactory _connectionFactory;
        private MessageSender _sender;
        private TestableMessageReceiver _receiver;

        [SetUp]
        public void Setup() {
            this._connectionFactory = System.Data.Entity.Database.DefaultConnectionFactory;
            this._sender = new MessageSender(this._connectionFactory, "TestSqlMessaging", "Test.Commands");
            this._receiver = new TestableMessageReceiver(this._connectionFactory);

            MessagingDbInitializer.CreateDatabaseObjects(
                this._connectionFactory.CreateConnection("TestSqlMessaging").ConnectionString, "Test", true);
        }

        [TearDown]
        public void TearDown() {
            using (var connection = this._connectionFactory.CreateConnection("TestSqlMessaging")) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "TRUNCATE TABLE Test.Commands";
                command.ExecuteNonQuery();
            }
        }

        [Test]
        public void WhenSendingMessageThenReceivesMessage() {
            Message message = null;
            this._receiver.MessageReceived += (s, e) => message = e.Message;

            this._sender.Send(new Message("test message"));

            Assert.IsTrue(this._receiver.ReceiveMessage());
            Assert.AreEqual("test message", message.Body);
            Assert.IsNull(message.CorrelationId);
            Assert.IsNull(message.DeliveryDate);
        }

        [Test]
        public void WhenSendingMessageWithCorrelationIdThenReceivesMessage() {
            Message message = null;
            this._receiver.MessageReceived += (s, e) => message = e.Message;

            this._sender.Send(new Message("test message", correlationId:"correlation"));

            Assert.IsTrue(this._receiver.ReceiveMessage());
            Assert.AreEqual("test message", message.Body);
            Assert.AreEqual("correlation", message.CorrelationId);
            Assert.IsNull(message.DeliveryDate);
        }

        [Test]
        public void WhenSuccessfullyHandlesMessageThenRemovesMessage() {
            this._receiver.MessageReceived += (s, e) => {};

            this._sender.Send(new Message("test message"));

            Assert.IsTrue(this._receiver.ReceiveMessage());
            Assert.IsFalse(this._receiver.ReceiveMessage());
        }

        [Test]
        public void WhenUnsuccessfullyHandlesMessageThenDoesNotRemoveMessage() {
            EventHandler<MessageReceivedEventArgs> failureHandler = null;
            failureHandler = (s, e) => {
                this._receiver.MessageReceived -= failureHandler;
                throw new ArgumentException();
            };

            this._receiver.MessageReceived += failureHandler;

            this._sender.Send(new Message("test message"));

            try {
                Assert.IsTrue(this._receiver.ReceiveMessage());
                Assert.IsFalse(true, "should have thrown");
            }
            catch(ArgumentException) {
            }

            Assert.IsTrue(this._receiver.ReceiveMessage());
        }

        [Test]
        public void WhenSendingMessageWithDelayThenReceivesMessageAfterDelay() {
            Message message = null;
            this._receiver.MessageReceived += (s, e) => message = e.Message;

            var deliverDate = DateTime.UtcNow.Add(TimeSpan.FromSeconds(5));
            this._sender.Send(new Message("test message", deliverDate));

            Assert.IsFalse(this._receiver.ReceiveMessage());
            Thread.Sleep(TimeSpan.FromSeconds(6));
            Assert.IsTrue(this._receiver.ReceiveMessage());
            Assert.AreEqual("test message", message.Body);
        }

        [Test]
        public void WhenReceivingMessageThenOtherReceiversCannotSeeMessageButSeeOtherMessages() {
            var secondReceiver = new TestableMessageReceiver(this._connectionFactory);

            this._sender.Send(new Message("message1"));
            this._sender.Send(new Message("message2"));

            var waitEvent = new AutoResetEvent(false);

            string receiver1Message = null;
            string receiver2Message = null;

            this._receiver.MessageReceived += (s, e) => {
                waitEvent.Set();
                receiver1Message = e.Message.Body;
                waitEvent.WaitOne();
            };

            secondReceiver.MessageReceived += (s, e) => {
                receiver2Message = e.Message.Body;
            };

            ThreadPool.QueueUserWorkItem(_ => this._receiver.ReceiveMessage());

            Assert.IsTrue(waitEvent.WaitOne(TimeSpan.FromSeconds(10)));
            secondReceiver.ReceiveMessage();
            waitEvent.Set();

            Assert.AreEqual("message1", receiver1Message);
            Assert.AreEqual("message2", receiver2Message);
        }

        [Test]
        public void WhenReceivingMessageThenCanSendNewMessage() {
            var secondReceiver = new TestableMessageReceiver(this._connectionFactory);

            this._sender.Send(new Message("message1"));

            var waitEvent = new AutoResetEvent(false);

            string receiver1Message = null;
            string receiver2Message = null;

            this._receiver.MessageReceived += (s, e) => {
                waitEvent.Set();
                receiver1Message = e.Message.Body;
                waitEvent.WaitOne();
            };

            secondReceiver.MessageReceived += (s, e) => {
                receiver2Message = e.Message.Body;
            };

            ThreadPool.QueueUserWorkItem(_ => this._receiver.ReceiveMessage());

            Assert.IsTrue(waitEvent.WaitOne(TimeSpan.FromSeconds(10)));

            this._sender.Send(new Message("message2"));
            secondReceiver.ReceiveMessage();
            waitEvent.Set();

            Assert.AreEqual("message1", receiver1Message);
            Assert.AreEqual("message2", receiver2Message);
        }

        public class TestableMessageReceiver : MessageReceiver {
            public TestableMessageReceiver(IDbConnectionFactory connectionFactory)
                : base(connectionFactory, "TestSqlMessaging", "Test.Commands") {
            }

            public new bool ReceiveMessage() {
                return base.ReceiveMessage();
            }
        }
    }
}