using System;
using System.Linq;
using System.Text;
using Infrastructure.EventSourcing;
using NUnit.Framework;
using Registration.Contracts;
using Registration.Events;

namespace Registration.Tests {
    public class GivenAnExistingReservation {
        private SeatsAvailability _sut;
        private Guid _conferenceId = Guid.NewGuid();
        private Guid _seatTypeId = Guid.NewGuid();
        private Guid _otherSeatTypeId = Guid.NewGuid();
        private Guid _reservationId = Guid.NewGuid();

        [SetUp]
        public void Setup() {
            this._sut = new SeatsAvailability(
                _conferenceId,
                new IVersionedEvent[] {
                    new AvailableSeatsChanged {
                        Seats = new [] {
                            new SeatQuantity(_seatTypeId, 10), 
                            new SeatQuantity(_otherSeatTypeId, 12) 
                        },
                        Version = 1
                    },
                    new SeatsReserved() {
                        ReservationId = _reservationId,
                        ReservationDetails = new []{new SeatQuantity(_seatTypeId, 6) },
                        AvailableSeatsChanged = new[]{new SeatQuantity(_seatTypeId, -6) },
                        Version = 2
                    }
                });
        }

        [Test]
        public void When_committing_then_commits_reservation_id() {
            _sut.CommitReservation(_reservationId);
            Assert.AreEqual(_reservationId, _sut.SingleEvent<SeatsReservationCommitted>().ReservationId);
        }

        [Test]
        public void When_cancelling_then_cancels_reservation_id() {
            _sut.CancelReservation(_reservationId);
            Assert.AreEqual(_reservationId, _sut.SingleEvent<SeatsReservationCancelled>().ReservationId);
        }

        [Test]
        public void When_cancelled_then_seats_become_available() {
            _sut.CancelReservation(_reservationId);

            Assert.AreEqual(_seatTypeId, _sut.SingleEvent<SeatsReservationCancelled>().AvailableSeatsChanged.Single().SeatType);
            Assert.AreEqual(6, _sut.SingleEvent<SeatsReservationCancelled>().AvailableSeatsChanged.Single().Quantity);
        }

        [Test]
        public void When_updating_reservation_with_more_seats_then_reserves_all_requested() {
            _sut.MakeReservation(_reservationId, new[]{new SeatQuantity(_seatTypeId, 8) });

            Assert.AreEqual(_reservationId, _sut.SingleEvent<SeatsReserved>().ReservationId);
            Assert.AreEqual(_seatTypeId, _sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().SeatType);
            Assert.AreEqual(8, _sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().Quantity);
        }

        [Test]
        public void When_updating_reservation_with_more_seats_then_changes_available_seats() {
            _sut.MakeReservation(_reservationId, new [] {new SeatQuantity(_seatTypeId, 8) });

            Assert.AreEqual(_seatTypeId, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().SeatType);
            Assert.AreEqual(-2, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().Quantity);
        }

        [Test]
        public void When_updating_reservation_with_less_seats_then_reserves_all_requested() {
            _sut.MakeReservation(_reservationId, new[] {new SeatQuantity(_seatTypeId, 2)});
            Assert.AreEqual(_reservationId, _sut.SingleEvent<SeatsReserved>().ReservationId);
            Assert.AreEqual(_seatTypeId, _sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().SeatType);
            Assert.AreEqual(2, _sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().Quantity);
        }

        [Test]
        public void When_updating_reservation_with_less_seats_then_changes_available_seats() {
            _sut.MakeReservation(_reservationId, new[] {new SeatQuantity(_seatTypeId, 2)});

            Assert.AreEqual(_seatTypeId, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().SeatType);
            Assert.AreEqual(4, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().Quantity);
        }

        [Test]
        public void When_updating_reservation_with_more_seats_than_available_then_reserves_as_much_as_possible() {
            _sut.MakeReservation(_reservationId, new[] {new SeatQuantity(_seatTypeId, 12)});

            Assert.AreEqual(_reservationId, _sut.SingleEvent<SeatsReserved>().ReservationId);
            Assert.AreEqual(_seatTypeId, _sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().SeatType);
            Assert.AreEqual(10, _sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().Quantity);

            Assert.AreEqual(_seatTypeId, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().SeatType);
            Assert.AreEqual(-4, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().Quantity);
        }

        [Test]
        public void When_updating_reservation_with_different_seats_then_reserves_them() {
            _sut.MakeReservation(_reservationId, new[]{new SeatQuantity(_otherSeatTypeId, 3) });

            Assert.AreEqual(_reservationId, _sut.SingleEvent<SeatsReserved>().ReservationId);
            Assert.AreEqual(_otherSeatTypeId, _sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().SeatType);
            Assert.AreEqual(3, _sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().Quantity);
        }

        [Test]
        public void When_updating_reservation_with_different_seats_then_unreserves_the_previous_ones_and_reserves_new_ones() {
            _sut.MakeReservation(_reservationId, new [] {new SeatQuantity(_otherSeatTypeId, 3) });

            Assert.AreEqual(2, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Count());
            Assert.AreEqual(-3, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single(x=>x.SeatType==_otherSeatTypeId).Quantity);
            Assert.AreEqual(6, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single(x=>x.SeatType==_seatTypeId).Quantity);
        }

        [Test]
        public void When_regenerating_from_memento_then_can_continue() {
            var memento = _sut.SaveToMemento();
            _sut = new SeatsAvailability(_sut.Id, memento, Enumerable.Empty<IVersionedEvent>());

            Assert.AreEqual(2, _sut.Version);

            _sut.MakeReservation(_reservationId, new[]{new SeatQuantity(_otherSeatTypeId, 3) });

            Assert.AreEqual(2, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Count());
            Assert.AreEqual(-3, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single(x=>x.SeatType==_otherSeatTypeId).Quantity);
            Assert.AreEqual(6, _sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single(x => x.SeatType == _seatTypeId).Quantity);
            Assert.AreEqual(3, _sut.SingleEvent<SeatsReserved>().Version);
        }
    }
}