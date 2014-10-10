using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Infrastructure.Messaging;
using Infrastructure.Sql.Database;
using Moq;
using NUnit.Framework;

namespace Infrastructure.Sql.IntegrationTests {
    [TestFixture]
    public class SqlDataContextFixture: IDisposable {
        public SqlDataContextFixture() {
            using(var dbContext = new TestDbContext()) {
                dbContext.Database.Delete();
                dbContext.Database.Create();
            }
        }

        [Test]
        public void WhenSavingAggregateRoot_ThenCanRetrieveIt() {
            var id = Guid.NewGuid();
            using(var context = new SqlDataContext<TestAggregateRoot>(()=>new TestDbContext(), Mock.Of<IEventBus>())) {
                var aggregateRoot = new TestAggregateRoot(id) {Title = "test"};
                context.Save(aggregateRoot);
            }
            
            using(var context = new SqlDataContext<TestAggregateRoot>(()=>new TestDbContext(), Mock.Of<IEventBus>())) {
                var aggregateRoot = context.Find(id);

                Assert.NotNull(aggregateRoot);
                Assert.AreEqual("test", aggregateRoot.Title);
            }
        }
        
        [Test]
        public void WhenSavingEntityTwice_ThenCanReloadIt() {
            var id = Guid.NewGuid();
            using(var context = new SqlDataContext<TestAggregateRoot>(()=>new TestDbContext(), Mock.Of<IEventBus>())) {
                var aggregateRoot = new TestAggregateRoot(id) ;
                context.Save(aggregateRoot);
            }
            
            using(var context = new SqlDataContext<TestAggregateRoot>(()=>new TestDbContext(), Mock.Of<IEventBus>())) {
                var aggregateRoot = context.Find(id);
                aggregateRoot.Title = "test";

                context.Save(aggregateRoot);
            }
            
            using(var context = new SqlDataContext<TestAggregateRoot>(()=>new TestDbContext(), Mock.Of<IEventBus>())) {
                var aggregateRoot = context.Find(id);
                Assert.AreEqual("test", aggregateRoot.Title);
            }
        }

        [Test]
        public void WhenEntityExposesEvent_ThenRepositoryPublishesIt() {
            var busMock = new Mock<IEventBus>();
            var events = new List<IEvent>();

            busMock.Setup(x => x.Publish(It.IsAny<IEnumerable<Envelope<IEvent>>>()))
                .Callback<IEnumerable<Envelope<IEvent>>>(x => events.AddRange(x.Select(e => e.Body)));

            var @event = new TestEvent();

            using(var context = new SqlDataContext<TestEventPublishingAggregateRoot>(()=>new TestDbContext(), busMock.Object)) {
                var aggregate = new TestEventPublishingAggregateRoot(Guid.NewGuid());
                aggregate.AddEvent(@event);

                context.Save(aggregate);
            }

            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events.Contains(@event));
        }

        public void Dispose() {
            using(var dbContext = new TestDbContext()) {
                dbContext.Database.Delete();
            }
        }

        public class TestDbContext: DbContext {
            public TestDbContext(): base("TestDbContext") {}

            public DbSet<TestAggregateRoot> TestAggregateRoots { get; set; } 
            public DbSet<TestEventPublishingAggregateRoot> TestEventPublishingAggregateRoots { get; set; } 
        }

        public class TestEvent: IEvent {
            public Guid SourceId { get; set; }
        }
    }
}