using System;
using System.Linq;
using System.Text;
using Infrastructure.EventSourcing;
using NUnit.Framework;
using Registration.Contracts;
using Registration.Events;

namespace Registration.Tests {
    [TestFixture]
    public class GivenSomeAvilableSeatsAndSomeTaken {
        private SeatsAvailability _sut;
        private Guid _conferenceId = Guid.NewGuid();
        private Guid _seatTypeId = Guid.NewGuid();
        private Guid _otherSeatTypeId = Guid.NewGuid();
        private Guid _reservationId = Guid.NewGuid();

        [SetUp]
        public void Setup() {
            this._sut = new SeatsAvailability(_conferenceId,
                new IVersionedEvent[] {
                    new AvailableSeatsChanged() {
                        Seats = new[] {
                            new SeatQuantity(_seatTypeId, 10),
                            new SeatQuantity(_otherSeatTypeId, 12)
                        }
                    },
                    new SeatsReserved() {
                        ReservationId = _reservationId,
                        ReservationDetails = new[] {new SeatQuantity(_seatTypeId, 6)},
                        AvailableSeatsChanged = new[] {new SeatQuantity(_seatTypeId, -6)}
                    }
                });
        }

        [Test]
        public void When_reserving_less_seats_than_remaining_then_seats_are_reserved() {
            _sut.MakeReservation(Guid.NewGuid(), new[] {new SeatQuantity(_seatTypeId, 4)});

            Assert.AreEqual(_seatTypeId, this._sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).SeatType);
            Assert.AreEqual(4, this._sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).Quantity);
            Assert.AreEqual(_seatTypeId, this._sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).SeatType);
            Assert.AreEqual(-4, this._sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).Quantity);
        }
        
        [Test]
        public void When_reserving_more_seats_than_remaining_then_reserves_all_remaining() {
            _sut.MakeReservation(Guid.NewGuid(), new[] {new SeatQuantity(_seatTypeId, 5)});

            Assert.AreEqual(_seatTypeId, this._sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).SeatType);
            Assert.AreEqual(4, this._sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).Quantity);
            Assert.AreEqual(_seatTypeId, this._sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).SeatType);
            Assert.AreEqual(-4, this._sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).Quantity);
        }

        [Test]
        public void When_cancelling_an_inexistent_reservation_then_no_op() {
            _sut.CancelReservation(Guid.NewGuid());
            Assert.AreEqual(0, _sut.Events.Count());
        }

        [Test]
        public void When_committing_inexistent_reservation_then_no_op() {
            _sut.CommitReservation(Guid.NewGuid());

            Assert.AreEqual(0, _sut.Events.Count());
        }
    }
}