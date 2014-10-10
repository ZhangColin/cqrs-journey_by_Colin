using System;
using System.Data.Entity.Infrastructure;
using Infrastructure.Sql.Messaging;
using Infrastructure.Sql.Messaging.Implementation;
using NUnit.Framework;

namespace Infrastructure.Sql.IntegrationTests.Messaging {
    [TestFixture]
    public class MessageSenderFixture {
        private IDbConnectionFactory _connectionFactory;
        private MessageSender _sender;

        [SetUp]
        public void Setup() {
            this._connectionFactory = System.Data.Entity.Database.DefaultConnectionFactory;
            this._sender = new MessageSender(this._connectionFactory, "TestSqlMessaging", "Test.Commands");

            MessagingDbInitializer.CreateDatabaseObjects(
                this._connectionFactory.CreateConnection("TestSqlMessaging").ConnectionString, "Test", true);
        }

        [TearDown]
        public void TearDown() {
            using(var connection = this._connectionFactory.CreateConnection("TestSqlMessaging")) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "TRUNCATE TABLE Test.Commands";
                command.ExecuteNonQuery();
            }
        }

        [Test]
        public void WhenSendingStringMessageThenSavesMessage() {
            var messageBody = "Message-" + Guid.NewGuid();
            var message = new Message(messageBody);

            this._sender.Send(message);
        }
    }
}