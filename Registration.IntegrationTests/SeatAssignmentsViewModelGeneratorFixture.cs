using System;
using System.Collections.Generic;
using Infrastructure.Serialization;
using Moq;
using NUnit.Framework;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Handlers;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Registration.IntegrationTests {
    [TestFixture]
    public class SeatAssignmentsViewModelGeneratorFixture {
        private string _dbName;

        private static readonly List<SeatTypeName> SeatTypes = new List<SeatTypeName>
        {
            new SeatTypeName { Id = Guid.NewGuid(), Name= "General" }, 
            new SeatTypeName { Id = Guid.NewGuid(), Name= "Precon" }, 
        };

        private static readonly Guid AssignmentsId = Guid.NewGuid();
        private static readonly Guid OrderId = Guid.NewGuid();
        private SeatAssignmentsViewModelGenerator sut;
        private IOrderDao dao;

        [SetUp]
        public void Setup()
        {
            this._dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new ConferenceRegistrationDbContext(this._dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            var conferenceDao = new Mock<IConferenceDao>();
            conferenceDao.Setup(x => x.GetSeatTypeNames(It.IsAny<IEnumerable<Guid>>()))
                .Returns(SeatTypes);

            var blobs = new MemoryBlobStorage();
            this.dao = new OrderDao(() => new ConferenceRegistrationDbContext(_dbName), blobs, new JsonTextSerializer());
            this.sut = new SeatAssignmentsViewModelGenerator(conferenceDao.Object, blobs, new JsonTextSerializer());

            this.sut.Handle(new SeatAssignmentsCreated {
                SourceId = AssignmentsId,
                OrderId = OrderId,
                Seats = new[]
                {
                    new SeatAssignmentsCreated.SeatAssignmentInfo { Position = 0, SeatType = SeatTypes[0].Id },
                    new SeatAssignmentsCreated.SeatAssignmentInfo { Position = 1, SeatType = SeatTypes[1].Id },
                }
            });
        }

        [TearDown]
        public void Dispose()
        {
            using (var context = new ConferenceRegistrationDbContext(this._dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();
            }
        }

        [Test]
        public void Then_creates_model_with_seat_names() {
            var dto = this.dao.FindOrderSeats(AssignmentsId);

            Assert.NotNull(dto);
            Assert.AreEqual(dto.Seats.Count, 2);
            Assert.AreEqual(SeatTypes[0].Name, dto.Seats[0].SeatName);
            Assert.AreEqual(0, dto.Seats[0].Position);
            Assert.AreEqual(SeatTypes[1].Name, dto.Seats[1].SeatName);
            Assert.AreEqual(1, dto.Seats[1].Position);
        }

        [Test]
        public void When_seat_assigned_then_sets_attendee() {
            this.sut.Handle(new SeatAssigned(AssignmentsId) {
                Position = 0,
                Attendee = new PersonalInfo {
                    Email = "a@b.com",
                    FirstName = "a",
                    LastName = "b",
                }
            });

            var dto = this.dao.FindOrderSeats(AssignmentsId);

            Assert.AreEqual("a@b.com", dto.Seats[0].Attendee.Email);
            Assert.AreEqual("a", dto.Seats[0].Attendee.FirstName);
            Assert.AreEqual("b", dto.Seats[0].Attendee.LastName);
        }

        [Test]
        public void When_assigned_seat_unassigned_then_clears_attendee_info() {
            this.sut.Handle(new SeatAssigned(AssignmentsId) {
                Position = 0,
                Attendee = new PersonalInfo {
                    Email = "a@b.com",
                    FirstName = "a",
                    LastName = "b",
                }
            });

            this.sut.Handle(new SeatUnassigned(AssignmentsId) { Position = 0 });

            var dto = this.dao.FindOrderSeats(AssignmentsId);

            Assert.IsNull(dto.Seats[0].Attendee.Email);
            Assert.IsNull(dto.Seats[0].Attendee.FirstName);
            Assert.IsNull(dto.Seats[0].Attendee.LastName);
        }

        [Test]
        public void When_assigned_seat_updated_then_sets_attendee_info() {
            this.sut.Handle(new SeatAssigned(AssignmentsId) {
                Position = 0,
                Attendee = new PersonalInfo {
                    Email = "a@b.com",
                    FirstName = "a",
                    LastName = "b",
                }
            });

            this.sut.Handle(new SeatAssignmentUpdated(AssignmentsId) {
                Position = 0,
                Attendee = new PersonalInfo {
                    Email = "b@c.com",
                    FirstName = "b",
                    LastName = "c",
                }
            });

            var dto = this.dao.FindOrderSeats(AssignmentsId);

            Assert.AreEqual("b@c.com", dto.Seats[0].Attendee.Email);
            Assert.AreEqual("b", dto.Seats[0].Attendee.FirstName);
            Assert.AreEqual("c", dto.Seats[0].Attendee.LastName);
        }
    }
}