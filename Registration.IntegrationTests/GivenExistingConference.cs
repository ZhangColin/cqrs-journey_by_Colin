using System;
using System.Collections.Generic;
using System.Linq;
using Conference.Contracts;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;
using Registration.Commands;
using Registration.Contracts;
using Registration.Events;
using Registration.Handlers;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Registration.IntegrationTests {
    using Registration.ReadModel;

    [TestFixture]
    public class GivenExistingConference {
        private string _dbName;
        private ConferenceViewModelGenerator _sut;
        private List<ICommand> _commands;

        private Guid _conferenceId = Guid.NewGuid();

        [SetUp]
        public void Setup() {
            this._dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            _commands = new List<ICommand>();

            var bus = new Mock<ICommandBus>();
            bus.Setup(x => x.Send(It.IsAny<Envelope<ICommand>>()))
                .Callback<Envelope<ICommand>>(x => this._commands.Add(x.Body));
            bus.Setup(x => x.Send(It.IsAny<IEnumerable<Envelope<ICommand>>>()))
                .Callback<IEnumerable<Envelope<ICommand>>>(x => this._commands.AddRange(x.Select(e => e.Body)));

            this._sut = new ConferenceViewModelGenerator(() => new ConferenceRegistrationDbContext(_dbName), bus.Object);

            System.Diagnostics.Trace.Listeners.Clear();

            this._sut.Handle(new ConferenceCreated {
                SourceId = _conferenceId,
                Name = "name",
                Description = "description",
                Slug = "test",
                Owner = new Owner {
                    Name = "owner",
                    Email = "owner@email.com",
                },
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
            });
        }

        [TearDown]
        public void Dispose() {
            using (var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if (context.Database.Exists())
                    context.Database.Delete();
            }
        }

        [Test]
        public void When_conference_updated_then_conference_dto_populated() {
            var startDate = new DateTimeOffset(2012, 04, 20, 15, 0, 0, TimeSpan.FromHours(-8));
            this._sut.Handle(new ConferenceUpdated {
                Name = "newname",
                Description = "newdescription",
                Slug = "newtest",
                Owner = new Owner {
                    Name = "owner",
                    Email = "owner@email.com",
                },
                SourceId = _conferenceId,
                StartDate = startDate.UtcDateTime,
                EndDate = DateTime.UtcNow.Date,
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Find<Conference>(_conferenceId);

                Assert.NotNull(dto);
                Assert.AreEqual("newname", dto.Name);
                Assert.AreEqual("newdescription", dto.Description);
                Assert.AreEqual("newtest", dto.Code);
                Assert.AreEqual(startDate, dto.StartDate);
            }
        }

        [Test]
        public void When_conference_published_then_conference_dto_updated() {
            var startDate = new DateTimeOffset(2012, 04, 20, 15, 0, 0, TimeSpan.FromHours(-8));
            this._sut.Handle(new ConferencePublished {
                SourceId = _conferenceId,
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Find<Conference>(_conferenceId);

                Assert.NotNull(dto);
                Assert.AreEqual(true, dto.IsPublished);
            }
        }

        [Test]
        public void When_published_conference_unpublished_then_conference_dto_updated() {
            this._sut.Handle(new ConferencePublished {
                SourceId = _conferenceId,
            });
            this._sut.Handle(new ConferenceUnpublished {
                SourceId = _conferenceId,
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Find<Conference>(_conferenceId);

                Assert.NotNull(dto);
                Assert.AreEqual(false, dto.IsPublished);
            }
        }

        [Test]
        public void When_seat_created_then_adds_seat() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Set<SeatType>()
                    .Where(x => x.ConferenceId == _conferenceId)
                    .Single(x => x.Id == seatId);

                Assert.AreEqual("seat", dto.Name);
                Assert.AreEqual("description", dto.Description);
                Assert.AreEqual(200, dto.Price);
                Assert.AreEqual(0, dto.AvailableQuantity);
                Assert.AreEqual(-1, dto.SeatsAvailabilityVersion);
            }
        }

        [Test]
        public void When_seat_created_then_add_seats_command_sent() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
                Quantity = 100,
            });

            var e = this._commands.OfType<AddSeats>().FirstOrDefault();

            Assert.NotNull(e);
            Assert.AreEqual(_conferenceId, e.ConferenceId);
            Assert.AreEqual(seatId, e.SeatType);
            Assert.AreEqual(100, e.Quantity);
        }

        [Test]
        public void When_seat_updated_then_updates_seat_on_conference_dto() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            this._sut.Handle(new SeatUpdated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "newseat",
                Description = "newdescription",
                Price = 100,
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Set<SeatType>()
                    .Where(x => x.ConferenceId == _conferenceId)
                    .Single(x => x.Id == seatId);

                Assert.NotNull(dto);
                Assert.AreEqual("newseat", dto.Name);
                Assert.AreEqual("newdescription", dto.Description);
                Assert.AreEqual(100, dto.Price);
                Assert.AreEqual(-1, dto.SeatsAvailabilityVersion);
            }
        }

        [Test]
        public void When_seats_added_then_add_seats_command_sent() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
                Quantity = 100,
            });

            this._commands.Clear();

            this._sut.Handle(new SeatUpdated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "newseat",
                Description = "newdescription",
                Price = 100,
                Quantity = 200,
            });

            var e = this._commands.OfType<AddSeats>().FirstOrDefault();

            Assert.NotNull(e);
            Assert.AreEqual(_conferenceId, e.ConferenceId);
            Assert.AreEqual(seatId, e.SeatType);
            Assert.AreEqual(100, e.Quantity);
        }

        [Test]
        public void When_seats_removed_then_add_seats_command_sent() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
                Quantity = 100,
            });

            this._commands.Clear();

            this._sut.Handle(new SeatUpdated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "newseat",
                Description = "newdescription",
                Price = 100,
                Quantity = 50,
            });

            var e = this._commands.OfType<RemoveSeats>().FirstOrDefault();

            Assert.NotNull(e);
            Assert.AreEqual(_conferenceId, e.ConferenceId);
            Assert.AreEqual(seatId, e.SeatType);
            Assert.AreEqual(50, e.Quantity);
        }

        [Test]
        public void When_available_seats_change_then_updates_remaining_quantity() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            this._sut.Handle(new AvailableSeatsChanged {
                SourceId = _conferenceId,
                Version = 1,
                Seats = new[] { new SeatQuantity (seatId, 200 ) }
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Set<SeatType>()
                    .Where(x => x.ConferenceId == _conferenceId)
                    .Single(x => x.Id == seatId);

                Assert.AreEqual("seat", dto.Name);
                Assert.AreEqual("description", dto.Description);
                Assert.AreEqual(200, dto.Price);
                Assert.AreEqual(200, dto.AvailableQuantity);
                Assert.AreEqual(1, dto.SeatsAvailabilityVersion);
            }
        }

        [Test]
        public void When_seats_are_reserved_then_updates_remaining_quantity() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            this._sut.Handle(new AvailableSeatsChanged {
                SourceId = _conferenceId,
                Version = 1,
                Seats = new[] { new SeatQuantity (seatId, 200 ) }
            });

            this._sut.Handle(new SeatsReserved {
                SourceId = _conferenceId,
                Version = 2,
                AvailableSeatsChanged = new[] { new SeatQuantity (seatId, -50 ) }
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Set<SeatType>()
                    .Where(x => x.ConferenceId == _conferenceId)
                    .Single(x => x.Id == seatId);

                Assert.AreEqual("seat", dto.Name);
                Assert.AreEqual("description", dto.Description);
                Assert.AreEqual(200, dto.Price);
                Assert.AreEqual(150, dto.AvailableQuantity);
                Assert.AreEqual(2, dto.SeatsAvailabilityVersion);
            }
        }

        [Test]
        public void When_seats_are_released_then_updates_remaining_quantity() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            this._sut.Handle(new AvailableSeatsChanged {
                SourceId = _conferenceId,
                Version = 1,
                Seats = new[] { new SeatQuantity (seatId, 200 ) }
            });

            this._sut.Handle(new SeatsReserved {
                SourceId = _conferenceId,
                Version = 2,
                AvailableSeatsChanged = new[] { new SeatQuantity (seatId, -50 ) }
            });

            this._sut.Handle(new SeatsReservationCancelled {
                SourceId = _conferenceId,
                Version = 3,
                AvailableSeatsChanged = new[] { new SeatQuantity (seatId, 50 ) }
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Set<SeatType>()
                    .Where(x => x.ConferenceId == _conferenceId)
                    .Single(x => x.Id == seatId);

                Assert.AreEqual("seat", dto.Name);
                Assert.AreEqual("description", dto.Description);
                Assert.AreEqual(200, dto.Price);
                Assert.AreEqual(200, dto.AvailableQuantity);
                Assert.AreEqual(3, dto.SeatsAvailabilityVersion);
            }
        }

        [Test]
        public void When_seat_availability_update_event_has_version_equal_to_last_update_then_event_is_ignored() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            this._sut.Handle(new AvailableSeatsChanged {
                SourceId = _conferenceId,
                Version = 1,
                Seats = new[] { new SeatQuantity (seatId, 200 ) }
            });

            this._sut.Handle(new SeatsReserved {
                SourceId = _conferenceId,
                Version = 2,
                AvailableSeatsChanged = new[] { new SeatQuantity (seatId, -50 ) }
            });

            this._sut.Handle(new SeatsReserved {
                SourceId = _conferenceId,
                Version = 2,
                AvailableSeatsChanged = new[] { new SeatQuantity (seatId, -50 ) }
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Set<SeatType>()
                    .Where(x => x.ConferenceId == _conferenceId)
                    .Single(x => x.Id == seatId);

                Assert.AreEqual("seat", dto.Name);
                Assert.AreEqual("description", dto.Description);
                Assert.AreEqual(200, dto.Price);
                Assert.AreEqual(150, dto.AvailableQuantity);
                Assert.AreEqual(2, dto.SeatsAvailabilityVersion);
            }
        }

        [Test]
        public void When_seat_availability_update_event_has_version_lower_than_last_update_then_event_is_ignored() {
            var seatId = Guid.NewGuid();

            this._sut.Handle(new SeatCreated {
                ConferenceId = _conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            this._sut.Handle(new AvailableSeatsChanged {
                SourceId = _conferenceId,
                Version = 0,
                Seats = new[] { new SeatQuantity (seatId, 200 ) }
            });

            this._sut.Handle(new SeatsReserved {
                SourceId = _conferenceId,
                Version = 1,
                AvailableSeatsChanged = new[] { new SeatQuantity (seatId, -50 ) }
            });

            this._sut.Handle(new AvailableSeatsChanged {
                SourceId = _conferenceId,
                Version = 0,
                Seats = new[] { new SeatQuantity (seatId, 200 ) }
            });

            using (var context = new ConferenceRegistrationDbContext(_dbName)) {
                var dto = context.Set<SeatType>()
                    .Where(x => x.ConferenceId == _conferenceId)
                    .Single(x => x.Id == seatId);

                Assert.AreEqual("seat", dto.Name);
                Assert.AreEqual("description", dto.Description);
                Assert.AreEqual(200, dto.Price);
                Assert.AreEqual(150, dto.AvailableQuantity);
                Assert.AreEqual(1, dto.SeatsAvailabilityVersion);
            }
        }
    }
}