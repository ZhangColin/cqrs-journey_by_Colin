using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Entity.Core;
using System.Linq;
using Conference.Contracts;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;

namespace Conference.IntegrationTests {
    [TestFixture]
    public class GivenNoConference {
        private string _dbName = "ConferenceServiceFixture_" + Guid.NewGuid();
        private ConferenceService _service;
        private List<IEvent> _busEvents;

        [SetUp]
        public void Setup() {
            using(var context = new ConferenceContext(_dbName)) {
                if(context.Database.Exists()) {
                    context.Database.Delete();
                }
                context.Database.Create();
            }

            this._busEvents = new List<IEvent>();
            var busMock = new Mock<IEventBus>();
            busMock.Setup(b => b.Publish(It.IsAny<Envelope<IEvent>>()))
                .Callback<Envelope<IEvent>>(e => _busEvents.Add(e.Body));
            busMock.Setup(b => b.Publish(It.IsAny<IEnumerable<Envelope<IEvent>>>()))
                .Callback<IEnumerable<Envelope<IEvent>>>(es => _busEvents.AddRange(es.Select(e => e.Body)));

            this._service = new ConferenceService(busMock.Object, this._dbName);
        }

        [TearDown]
        public void TearDown() {
            using (var context = new ConferenceContext(_dbName)) {
                if (context.Database.Exists()) {
                    context.Database.Delete();
                }
            }
        }

        [Test]
        public void When_finding_by_non_existing_slug_then_returns_null() {
            var conference = _service.FindConference(Guid.NewGuid().ToString());
            Assert.IsNull(conference);
        }

        [Test]
        public void When_finding_by_non_existing_email_and_access_code_then_returns_null() {
            var conference = _service.FindConference("foo@bar.com", Guid.NewGuid().ToString());
            Assert.IsNull(conference);
        }

        [Test]
        public void When_finding_seats_by_non_existing_conference_id_then_returns_empty() {
            var conference = _service.FindSeatTypes(Guid.NewGuid());
            Assert.IsEmpty(conference);
        }

        [Test]
        public void When_creating_seat_then_throws() {
            Assert.Throws<ObjectNotFoundException>(() => _service.CreateSeat(Guid.NewGuid(), new SeatType()));
        }

        [Test]
        public void When_updating_non_existing_conference_then_throws() {
            Assert.Throws<ObjectNotFoundException>(() => _service.UpdateConference(new ConferenceInfo()));
        }

        [Test]
        public void When_updating_seat_for_non_existing_conference_then_throws() {
            Assert.Throws<ObjectNotFoundException>(() => _service.UpdateSeat(Guid.NewGuid(), new SeatType()));
        }

        [Test]
        public void When_updating_published_non_existing_conference_then_throws() {
            Assert.Throws<ObjectNotFoundException>(() => _service.Unpublish(Guid.NewGuid()));
        }

        [Test]
        public void When_deleting_non_existing_seat_then_throws() {
            Assert.Throws<ObjectNotFoundException>(() => _service.DeleteSeat(Guid.NewGuid()));
        }

        [Test]
        public void When_creating_conference_and_seat_then_does_not_publish_seat_created() {
            var conference = new ConferenceInfo() {
                OwnerEmail = "test@contoso.com",
                OwnerName = "test owner",
                AccessCode = "qwerty",
                Name = "test conference",
                Description = "test conference description",
                Location = "redmond",
                Slug = "test",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.Add(TimeSpan.FromDays(2))
            };

            _service.CreateConference(conference);

            var seat = new SeatType() {
                Name = "seat",
                Description = "description",
                Price = 100,
                Quantity = 100
            };
            _service.CreateSeat(conference.Id, seat);

            Assert.IsEmpty(_busEvents.OfType<SeatCreated>());
        }

        [Test]
        public void When_creating_conference_and_seat_then_publishes_seat_created_on_publish() {
            var conference = new ConferenceInfo() {
                OwnerEmail = "test@contoso.com",
                OwnerName = "test owner",
                AccessCode = "qwerty",
                Name = "test conference",
                Description = "test conference description",
                Location = "redmond",
                Slug = "test",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.Add(TimeSpan.FromDays(2))
            };

            _service.CreateConference(conference);

            var seat = new SeatType() {
                Name = "seat",
                Description = "description",
                Price = 100,
                Quantity = 100
            };
            _service.CreateSeat(conference.Id, seat);

            _service.Publish(conference.Id);

            var e = _busEvents.OfType<SeatCreated>().FirstOrDefault();

            Assert.NotNull(e);
            Assert.AreEqual(e.SourceId, seat.Id);
        }
    }
}