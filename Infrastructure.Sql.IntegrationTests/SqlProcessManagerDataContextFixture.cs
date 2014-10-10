using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using Infrastructure.Messaging;
using Infrastructure.Processes;
using Infrastructure.Serialization;
using Infrastructure.Sql.Processes;
using Moq;
using NUnit.Framework;

namespace Infrastructure.Sql.IntegrationTests {
    [TestFixture]
    public class SqlProcessManagerDataContextFixture {
        protected readonly string DbName = typeof(SqlProcessManagerDataContextFixture).Name + "-" + Guid.NewGuid();

        [SetUp]
        public void Setup() {
            using(var context = new TestProcessManagerDbContext(this.DbName)) {
                context.Database.Delete();
                context.Database.Create();
            }
        }

        [TearDown]
        public void TearDown() {
            using(var context = new TestProcessManagerDbContext(this.DbName)) {
                context.Database.Delete();
            }
        }

        [Test]
        public void WhenSavingEntity_ThenCanRetrieveIt() {
            var id = Guid.NewGuid();

            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>())) {
                var conference = new OrmTestProcessManager(id);
                context.Save(conference);
            }
            
            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>())) {
                var conference = context.Find(id);
                Assert.NotNull(conference);
            }
        }
        
        [Test]
        public void WhenSavingEntityTwice_ThenCanReloadIt() {
            var id = Guid.NewGuid();

            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>())) {
                var conference = new OrmTestProcessManager(id);
                context.Save(conference);
            }
            
            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>())) {
                var conference = context.Find(id);
                conference.Title = "CQRS Journey";

                context.Save(conference);
            }
            
            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>())) {
                var conference = context.Find(id);
                Assert.AreEqual("CQRS Journey", conference.Title);
            }
        }
        
        [Test]
        public void WhenSavingWithConcurrencyConflict_ThenThrowsException() {
            var id = Guid.NewGuid();

            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>())) {
                var conference = new OrmTestProcessManager(id);
                context.Save(conference);
            }
            
            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>())) {
                var conference = context.Find(id);
                conference.Title = "CQRS Journey";

                using (var innerContext = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>())) {
                    var innerConference = innerContext.Find(id);
                    innerConference.Title = "CQRS Journey!!";

                    innerContext.Save(innerConference);
                }

                Assert.Throws<ConcurrencyException>(() => context.Save(conference));
            }
        }

        [Test]
        public void WhenEntityExposesCommand_ThenRepositoryPublishesIt() {
            var bus = new Mock<ICommandBus>();
            var commands = new List<ICommand>();

            bus.Setup(x => x.Send(It.IsAny<Envelope<ICommand>>()))
                .Callback<Envelope<ICommand>>(x => commands.Add(x.Body));

            var command = new TestCommand();

            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), bus.Object, Mock.Of<ITextSerializer>())) {
                var aggregate = new OrmTestProcessManager(Guid.NewGuid());
                aggregate.AddCommand(command);

                context.Save(aggregate);
            }

            Assert.AreEqual(1, commands.Count);
            Assert.IsTrue(commands.Contains(command));
        }

        [Test]
        public void WhenCommandPublishingThrows_ThenPublishesPendingCommandOnNextFind() {
            var bus = new Mock<ICommandBus>();
            var command1 = new Envelope<ICommand>(new TestCommand());
            var command2 = new Envelope<ICommand>(new TestCommand());

            var id = Guid.NewGuid();

            bus.Setup(x => x.Send(command2)).Throws<TimeoutException>();


            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), bus.Object, new JsonTextSerializer())) {
                var aggregate = new OrmTestProcessManager(id);
                aggregate.AddEnvelope(command1, command2);

                Assert.Throws<TimeoutException>(() => context.Save(aggregate));
            }

            bus.Verify(x => x.Send(command1));
            bus.Verify(x => x.Send(command2));

            bus = new Mock<ICommandBus>();

            using (var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), bus.Object, new JsonTextSerializer())) {
                var aggregate = context.Find(id);

                Assert.NotNull(aggregate);
                bus.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==command1.Body.Id)), Times.Never());
                bus.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==command2.Body.Id)));
            }
        }

        [Test]
        public void WhenCommandPublishingThrowsOnFind_ThenThrows() {
            var bus = new Mock<ICommandBus>();
            var command1 = new Envelope<ICommand>(new TestCommand());
            var command2 = new Envelope<ICommand>(new TestCommand());

            var id = Guid.NewGuid();

            bus.Setup(x => x.Send(command2)).Throws<TimeoutException>();


            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), bus.Object, new JsonTextSerializer())) {
                var aggregate = new OrmTestProcessManager(id);
                aggregate.AddEnvelope(command1, command2);

                Assert.Throws<TimeoutException>(() => context.Save(aggregate));
            }

            using (var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), bus.Object, new JsonTextSerializer())) {
                bus.Setup(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command2.Body.Id)))
                    .Throws<TimeoutException>();
                Assert.Throws<TimeoutException>(() => context.Find(id));
            }
        }

        [Test]
        public void WhenCommandPublishingFails_ThenThrows() {
            var bus = new Mock<ICommandBus>();
            var command1 = new Envelope<ICommand>(new TestCommand());
            var command2 = new Envelope<ICommand>(new TestCommand());
            var command3 = new Envelope<ICommand>(new TestCommand());

            var id = Guid.NewGuid();

            bus.Setup(x => x.Send(command2)).Throws<TimeoutException>();


            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), bus.Object, new JsonTextSerializer())) {
                var aggregate = new OrmTestProcessManager(id);
                aggregate.AddEnvelope(command1, command2, command3);

                Assert.Throws<TimeoutException>(() => context.Save(aggregate));
            }
        }

        [Test]
        public void WhenCommandPublishingThrowsPartiallyOnSave_ThenPublishesPendingCommandOnNextFind() {
            var bus = new Mock<ICommandBus>();
            var command1 = new Envelope<ICommand>(new TestCommand());
            var command2 = new Envelope<ICommand>(new TestCommand());
            var command3 = new Envelope<ICommand>(new TestCommand());

            var id = Guid.NewGuid();

            bus.Setup(x => x.Send(command2)).Throws<TimeoutException>();

            using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), bus.Object, new JsonTextSerializer())) {
                var aggregate = new OrmTestProcessManager(id);
                aggregate.AddEnvelope(command1, command2, command3);

                Assert.Throws<TimeoutException>(() => context.Save(aggregate));
            }

            bus.Verify(x=>x.Send(command1));
            bus.Verify(x=>x.Send(command2));
            bus.Verify(x=>x.Send(command3), Times.Never);

            bus.Setup(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command3.Body.Id)))
                .Throws<TimeoutException>();

            using (var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), bus.Object, new JsonTextSerializer())) {
                Assert.Throws<TimeoutException>(() => context.Find(id));
                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command2.Body.Id)));
                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command3.Body.Id)));
            }

            bus = new Mock<ICommandBus>();
            using (var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(() => new TestProcessManagerDbContext(this.DbName), bus.Object, new JsonTextSerializer())) {
                var aggregate = context.Find(id);

                Assert.NotNull(aggregate);
                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command2.Body.Id)), Times.Never());
                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command3.Body.Id)));
            }
        }



        
    }

    public class TestProcessManagerDbContext : DbContext {
        public TestProcessManagerDbContext(string nameOrConnectionString) : base(nameOrConnectionString) { }

        public DbSet<OrmTestProcessManager> OrmTestProcessManagers { get; set; }
        public DbSet<UndispatchedMessages> UndispatchedMessages { get; set; }
    }

    public class TestCommand : ICommand {
        public TestCommand() {
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
    }

    public class OrmTestProcessManager:IProcessManager  {
        private readonly  List<Envelope<ICommand>> _commands = new List<Envelope<ICommand>>(); 
        protected OrmTestProcessManager() { }

        public OrmTestProcessManager(Guid id) {
            this.Id = id;
        }

        public Guid Id { get; private set; }
        public bool Completed { get; private set; }

        public string Title { get; set; }

        [ConcurrencyCheck]
        [Timestamp]
        public byte[] TimeStamp { get; private set; }

        public IEnumerable<Envelope<ICommand>> Commands { get { return _commands; } }

        public void AddCommand(ICommand command) {
            this._commands.Add(Envelope.Create(command));
        }

        public void AddEnvelope(params Envelope<ICommand>[] commands) {
            this._commands.AddRange(commands);
        }
    }
}