using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;
using Registration.Contracts.Events;

namespace Conference.IntegrationTests {
    [TestFixture]
    public class GivenNoOrder {
        private string _dbName = "OrderEventHandlerFixture_" + Guid.NewGuid();
        private OrderEventHandler _eventHandler;
        
        [SetUp]
        public void Setup() {
            using(var context = new ConferenceContext(_dbName)) {
                if(context.Database.Exists()) {
                    context.Database.Delete();
                }
                context.Database.Create();
            }

            this._eventHandler = new OrderEventHandler(() => new ConferenceContext(_dbName));
        }

        [TearDown]
        public void Dispose() {
            using(var context = new ConferenceContext(_dbName)) {
                if(context.Database.Exists()) {
                    context.Database.Delete();
                }
            }
        }

        [Test]
        public void When_order_placed_then_creates_order_entity() {
            var e = new OrderPlaced {
                ConferenceId = Guid.NewGuid(),
                SourceId = Guid.NewGuid(),
                AccessCode = "asdf"
            };

            this._eventHandler.Handle(e);

            using(var context = new ConferenceContext(_dbName)) {
                var order = context.Orders.Find(e.SourceId);
                Assert.NotNull(order);
            }
        }
    }
}