using System;
using System.Collections.Generic;
using System.Linq;
using Conference.Contracts;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;
using Registration.Handlers;
using Registration.ReadModel.Implementation;

namespace Registration.IntegrationTests {
    using Registration.ReadModel;

    [TestFixture]
    public class GivenNoConference {
        private string _dbName;
        private ConferenceViewModelGenerator _sut;
        private readonly List<ICommand> _commands = new List<ICommand>();

        [SetUp]
        public void Setup() {
            this._dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            var bus = new Mock<ICommandBus>();
            bus.Setup(x => x.Send(It.IsAny<Envelope<ICommand>>()))
                .Callback<Envelope<ICommand>>(x => this._commands.Add(x.Body));
            bus.Setup(x => x.Send(It.IsAny<IEnumerable<Envelope<ICommand>>>()))
                .Callback<IEnumerable<Envelope<ICommand>>>(x => this._commands.AddRange(x.Select(e => e.Body)));

            this._sut = new ConferenceViewModelGenerator(() => new ConferenceRegistrationDbContext(_dbName), bus.Object);
        }

        [TearDown]
        public void Dispose() {
            using (var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if (context.Database.Exists())
                    context.Database.Delete();
            }
        }

        [Test]
        public void When_conference_created_then_conference_dto_populated() {
            var conferenceId = Guid.NewGuid();

            this._sut.Handle(new ConferenceCreated {
                Name = "name",
                Description = "description",
                Slug = "test",
                Owner = new Owner {
                    Name = "owner",
                    Email = "owner@email.com",
                },
                SourceId = conferenceId,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Find<Conference>(conferenceId);

                Assert.NotNull(dto);
                Assert.AreEqual("name", dto.Name);
                Assert.AreEqual("description", dto.Description);
                Assert.AreEqual("test", dto.Code);
            }
        }

        [Test]
        public void When_seat_created_even_when_conference_created_was_not_handled_then_creates_seat() {
            var conferenceId = Guid.NewGuid();
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Set<SeatType>()
                    .FirstOrDefault(x => x.Id == seatId);

                Assert.NotNull(dto);
                Assert.AreEqual("seat", dto.Name);
                Assert.AreEqual("description", dto.Description);
                Assert.AreEqual(conferenceId, dto.ConferenceId);
                Assert.AreEqual(200, dto.Price);
                Assert.AreEqual(0, dto.AvailableQuantity);
            }
        }
    }
}