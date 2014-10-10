using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core;
using System.Linq;
using Conference.Contracts;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;

namespace Conference.IntegrationTests {
    [TestFixture]
    public class GivenAnExistingConferenceWithASeat {
        private string _dbName = "ConferenceServiceFixture_" + Guid.NewGuid();
        private ConferenceService _service;
        private List<IEvent> _busEvents;
        private ConferenceInfo _conference;

        [SetUp]
        public void Setup() {
            // 初始化会议数据库
            using (var context = new ConferenceContext(this._dbName)) {
                if (context.Database.Exists()) {
                    context.Database.Delete();
                }
                context.Database.Create();
            }

            // 模拟事件总线
            this._busEvents = new List<IEvent>();
            var busMock = new Mock<IEventBus>();
            busMock.Setup(b => b.Publish(It.IsAny<Envelope<IEvent>>()))
                .Callback<Envelope<IEvent>>(e => this._busEvents.Add(e.Body));
            busMock.Setup(b => b.Publish(It.IsAny<IEnumerable<Envelope<IEvent>>>()))
                .Callback<IEnumerable<Envelope<IEvent>>>(es => this._busEvents.AddRange(es.Select(e => e.Body)));

            this._service = new ConferenceService(busMock.Object, this._dbName);

            this._conference = new ConferenceInfo() {
                OwnerEmail = "test@contoso.com",
                OwnerName = "test owner",
                AccessCode = "qwerty",
                Name = "test conference",
                Description = "test conference description",
                Location = "redmond",
                Slug = "test",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.Add(TimeSpan.FromDays(2)),
                Seats = {
                    new SeatType() {
                        Name = "general", 
                        Description = "general description", 
                        Price = 100, 
                        Quantity = 10
                    }
                }
            };

            this._service.CreateConference(this._conference);
        }

        [TearDown]
        public void TearDown() {
            using (var context = new ConferenceContext(this._dbName)) {
                if (context.Database.Exists()) {
                    context.Database.Delete();
                }
            }
        }

        [Test]
        public void Then_conference_is_created_unpublished() {
            using(var context = new ConferenceContext(this._dbName)) {
                Assert.IsFalse(context.Conferences.Find(this._conference.Id).IsPublished);
                Assert.IsFalse(context.Conferences.Find(this._conference.Id).WasEverPublished);
            }
        }
        
        [Test]
        public void Then_conference_is_persisted() {
            using(var context = new ConferenceContext(this._dbName)) {
                Assert.NotNull(context.Conferences.Find(this._conference.Id));
            }
        }

        [Test]
        public void Then_conference_created_is_published() {
            var e = this._busEvents.OfType<ConferenceCreated>().Single();

            Assert.NotNull(e);
            Assert.AreEqual(this._conference.Id, e.SourceId);
        }

        [Test]
        public void Then_seat_created_is_published_on_publish() {
            this._service.Publish(this._conference.Id);

            var e = _busEvents.OfType<SeatCreated>().Single();
            var seat = this._conference.Seats.Single();

            Assert.AreEqual(seat.Id, e.SourceId);
            Assert.AreEqual(seat.Name, e.Name);
            Assert.AreEqual(seat.Description, e.Description);
            Assert.AreEqual(seat.Price, e.Price);
            Assert.AreEqual(seat.Quantity, e.Quantity);
        }

        [Test]
        public void When_finding_by_slug_then_returns_conference() {
            var conference = this._service.FindConference(this._conference.Slug);
            Assert.NotNull(conference);
        }
        
        [Test]
        public void When_finding_by_existing_email_and_access_code_then_returns_conference() {
            var conference = this._service.FindConference(this._conference.OwnerEmail, this._conference.AccessCode);
            Assert.NotNull(conference);
        }

        [Test]
        public void When_finding_seats_by_non_existing_conference_id_then_returns_empty() {
            var conference = this._service.FindSeatTypes(this._conference.Id);
            Assert.IsNotEmpty(conference);
        }

        [Test]
        public void When_creating_conference_with_existing_slug_then_throws() {
            this._conference.Id = Guid.NewGuid();
            Assert.Throws<DuplicateNameException>(() => _service.CreateConference(this._conference));
        }

        [Test]
        public void When_creating_seat_then_adds_to_conference() {
            var seat = new SeatType() {
                Name = "precon",
                Description = "perecon desc",
                Price = 100,
                Quantity = 100
            };

            _service.CreateSeat(this._conference.Id, seat);

            var seats = _service.FindSeatTypes(this._conference.Id);

            Assert.AreEqual(2, seats.Count());
        }

        [Test]
        public void When_creating_seat_then_seat_created_is_published() {
            this._service.Publish(this._conference.Id);

            var seat = new SeatType() {
                Name = "precon",
                Description = "perecon desc",
                Price = 100,
                Quantity = 100
            };

            _service.CreateSeat(this._conference.Id, seat);

            var e = this._busEvents.OfType<SeatCreated>().Single(x => x.SourceId == seat.Id);

            Assert.AreEqual(this._conference.Id, e.ConferenceId);
            Assert.AreEqual(seat.Id, e.SourceId);
            Assert.AreEqual(seat.Name, e.Name);
            Assert.AreEqual(seat.Description, e.Description);
            Assert.AreEqual(seat.Price, e.Price);
            Assert.AreEqual(seat.Quantity, e.Quantity);
        }

        [Test]
        public void When_creating_seat_then_can_find_seat() {
            var seat = new SeatType() {
                Name = "precon",
                Description = "perecon desc",
                Price = 100,
                Quantity = 100
            };

            this._service.CreateSeat(this._conference.Id, seat);

            Assert.NotNull(_service.FindSeatType(seat.Id));
        }

        [Test]
        public void When_updating_conference_then_can_find_updated_information() {
            this._conference.Name = "foo";
            this._conference.Description = "bar";
            this._conference.Seats.Clear();

            this._service.UpdateConference(this._conference);

            var saved = this._service.FindConference(this._conference.Slug);

            Assert.AreEqual(this._conference.Name, saved.Name);
            Assert.AreEqual(this._conference.Description, saved.Description);
        }

        [Test]
        public void When_updating_non_existing_seat_then_throws() {
            Assert.Throws<ObjectNotFoundException>(() => _service.UpdateSeat(this._conference.Id, new SeatType()));
        }

        [Test]
        public void When_updating_seat_then_can_find_updated_information() {
            var seat = this._conference.Seats.First();
            seat.Name = "precon";
            seat.Description = "precon desc";
            seat.Price = 200;

            _service.UpdateSeat(this._conference.Id, seat);

            var saved = _service.FindSeatType(seat.Id);

            Assert.AreEqual(seat.Name, saved.Name);
            Assert.AreEqual(seat.Description, saved.Description);
            Assert.AreEqual(seat.Quantity, saved.Quantity);
        }

        [Test]
        public void When_updating_seat_then_seat_updated_event_is_published() {
            this._service.Publish(this._conference.Id);

            var seat = this._conference.Seats.First();
            seat.Name = "precon";
            seat.Description = "precon desc";
            seat.Price = 200;
            seat.Quantity = 1000;

            _service.UpdateSeat(this._conference.Id, seat);

            var e = this._busEvents.OfType<SeatUpdated>().LastOrDefault();

            Assert.AreEqual(this._conference.Id, e.ConferenceId);
            Assert.AreEqual(seat.Id, e.SourceId);
            Assert.AreEqual("precon", e.Name);
            Assert.AreEqual("precon desc", e.Description);
            Assert.AreEqual(200, e.Price);
            Assert.AreEqual(1000, e.Quantity);
        }

        [Test]
        public void When_updating_published_updates_conference() {
            _service.Publish(this._conference.Id);
            Assert.IsTrue(_service.FindConference(this._conference.Slug).IsPublished);
            
            _service.Unpublish(this._conference.Id);
            Assert.IsFalse(_service.FindConference(this._conference.Slug).IsPublished);
        }

        [Test]
        public void When_updating_published_then_sets_conference_ever_published() {
            _service.Publish(this._conference.Id);

            Assert.IsTrue(_service.FindConference(this._conference.Slug).WasEverPublished);
        }

        [Test]
        public void When_updating_published_to_false_then_conference_ever_published_remains_true() {
            _service.Publish(this._conference.Id);
            _service.Unpublish(this._conference.Id);

            Assert.IsTrue(_service.FindConference(this._conference.Slug).WasEverPublished);
        }

        [Test]
        public void When_deleting_seat_then_updates_conference_seats() {
            Assert.AreEqual(1, _service.FindSeatTypes(this._conference.Id).Count());
            _service.DeleteSeat(this._conference.Seats.First().Id);
            Assert.AreEqual(0, _service.FindSeatTypes(this._conference.Id).Count());
        }

        [Test]
        public void When_deleting_seat_from_published_conference_then_throws() {
            _service.Publish(this._conference.Id);
            Assert.Throws<InvalidOperationException>(() => _service.DeleteSeat(this._conference.Seats.First().Id));
        }

        [Test]
        public void When_deleting_seat_from_previously_published_conference_then_throws() {
            _service.Publish(this._conference.Id);
            _service.Unpublish(this._conference.Id);

            Assert.Throws<InvalidOperationException>(() => _service.DeleteSeat(this._conference.Seats.First().Id));
        }
    }
}