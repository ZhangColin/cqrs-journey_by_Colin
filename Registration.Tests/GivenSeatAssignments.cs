using System;
using System.Linq;
using Infrastructure.EventSourcing;
using Moq;
using NUnit.Framework;
using Registration.Commands;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Handlers;

namespace Registration.Tests {
    [TestFixture]
    public class GivenSeatAssignments {
        protected Guid _assignmentsId = Guid.NewGuid();
        protected Guid _orderId = Guid.NewGuid();
        protected Guid _seatType = Guid.NewGuid();
        protected EventSourcingTestHelper<SeatAssignments> _sut;

        [SetUp]
        public void Setup() {
            this._sut = new EventSourcingTestHelper<SeatAssignments>();
            this._sut.Setup(new SeatAssignmentsHandler(Mock.Of<IEventSourcedRepository<Order>>(), this._sut.Repository));
            this._sut.Given(new SeatAssignmentsCreated {
                SourceId = this._assignmentsId,
                OrderId = this._orderId,
                Seats = Enumerable.Range(0, 5).Select(i =>
                    new SeatAssignmentsCreated.SeatAssignmentInfo {
                        Position = i,
                        SeatType = this._seatType,
                    })
            },
            new SeatAssigned(this._assignmentsId) {
                Position = 0,
                SeatType = this._seatType,
                Attendee = new PersonalInfo {
                    Email = "a@a.com",
                    FirstName = "A",
                    LastName = "Z",
                }
            });
        }

        [Test]
        public void When_assigning_unassigned_seat_then_seat_is_assigned() {
            var command = new AssignSeat {
                SeatAssignmentsId = this._assignmentsId,
                Position = 1,
                Attendee = new PersonalInfo {
                    Email = "a@a.com",
                    FirstName = "A",
                    LastName = "Z",
                }
            };
            this._sut.When(command);

            var @event = this._sut.ThenHasSingle<SeatAssigned>();

            Assert.AreEqual(1, @event.Position);
            Assert.AreEqual(this._seatType, @event.SeatType);
            Assert.AreEqual(this._assignmentsId, @event.SourceId);
            Assert.AreEqual(command.Attendee, @event.Attendee);
        }

        [Test]
        public void When_unassigning_seat_then_seat_is_unassigned() {
            var command = new UnassignSeat {
                SeatAssignmentsId = this._assignmentsId,
                Position = 0,
            };
            this._sut.When(command);

            var @event = this._sut.ThenHasSingle<SeatUnassigned>();

            Assert.AreEqual(0, @event.Position);
            Assert.AreEqual(this._assignmentsId, @event.SourceId);
        }

        [Test]
        public void When_unassigning_already_unnassigned_seat_then_no_event_is_raised() {
            var command = new UnassignSeat {
                SeatAssignmentsId = this._assignmentsId,
                Position = 1,
            };
            this._sut.When(command);

            Assert.False(this._sut.Events.OfType<SeatUnassigned>().Any());
        }

        [Test]
        public void When_assigning_previously_assigned_seat_to_new_email_then_reassigns_seat_with_two_events() {
            var command = new AssignSeat {
                SeatAssignmentsId = this._assignmentsId,
                Position = 0,
                Attendee = new PersonalInfo {
                    Email = "b@b.com",
                    FirstName = "B",
                    LastName = "Z",
                }
            };
            this._sut.When(command);

            var unassign = this._sut.ThenHasOne<SeatUnassigned>();

            Assert.AreEqual(0, unassign.Position);
            Assert.AreEqual(this._assignmentsId, unassign.SourceId);

            var assign = this._sut.ThenHasOne<SeatAssigned>();

            Assert.AreEqual(0, assign.Position);
            Assert.AreEqual(this._seatType, assign.SeatType);
            Assert.AreEqual(this._assignmentsId, assign.SourceId);
            Assert.AreEqual(command.Attendee, assign.Attendee);
        }

        [Test]
        public void When_assigning_previously_assigned_seat_to_same_email_then_updates_assignment() {
            var command = new AssignSeat {
                SeatAssignmentsId = this._assignmentsId,
                Position = 0,
                Attendee = new PersonalInfo {
                    Email = "a@a.com",
                    FirstName = "B",
                    LastName = "Z",
                }
            };
            this._sut.When(command);

            var assign = this._sut.ThenHasSingle<SeatAssignmentUpdated>();

            Assert.AreEqual(0, assign.Position);
            Assert.AreEqual(this._assignmentsId, assign.SourceId);
            Assert.AreEqual(command.Attendee, assign.Attendee);
        }
    }
}