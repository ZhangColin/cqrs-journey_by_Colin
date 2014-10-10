using System;
using System.Linq;
using NUnit.Framework;
using Registration.Contracts;
using Registration.Events;

namespace Registration.Tests {
    [TestFixture]
    public class GivenAvailableSeats {
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();

        private SeatsAvailability _sut;

        [SetUp]
        public void Setup() {
            this._sut = new SeatsAvailability(ConferenceId,
                new[] {new AvailableSeatsChanged {Seats = new[] {new SeatQuantity(SeatTypeId, 10)}}});
        }

        [Test]
        public void When_adding_seat_type_then_changes_availability() {
            var id = Guid.NewGuid();
            var seatType = Guid.NewGuid();

            var seatsAvailability = new SeatsAvailability(id);
            seatsAvailability.AddSeats(seatType, 50);

            Assert.AreEqual(seatType, seatsAvailability.SingleEvent<AvailableSeatsChanged>().Seats.Single().SeatType);
            Assert.AreEqual(50, seatsAvailability.SingleEvent<AvailableSeatsChanged>().Seats.Single().Quantity);
        }

        [Test]
        public void When_adding_non_existing_seat_type_then_adds_availability() {
            var seatType = Guid.NewGuid();
            _sut.AddSeats(seatType, 50);

            Assert.AreEqual(seatType, _sut.SingleEvent<AvailableSeatsChanged>().Seats.Single().SeatType);
            Assert.AreEqual(50, _sut.SingleEvent<AvailableSeatsChanged>().Seats.Single().Quantity);
        }

        [Test]
        public void When_adding_seats_to_existing_seat_type_then_adds_remaining_seats() {
            _sut.AddSeats(SeatTypeId, 10);

            Assert.AreEqual(SeatTypeId, ((AvailableSeatsChanged)_sut.Events.Single()).Seats.Single().SeatType);
            Assert.AreEqual(10, ((AvailableSeatsChanged)_sut.Events.Single()).Seats.Single().Quantity);
        }

        [Test]
        public void When_removing_seats_to_existing_seat_type_then_removes_remaining_seats() {
            this._sut.RemoveSeats(SeatTypeId, 5);
            this._sut.MakeReservation(Guid.NewGuid(), new []{new SeatQuantity(SeatTypeId, 10)});

            Assert.AreEqual(SeatTypeId, this._sut.Events.OfType<AvailableSeatsChanged>().Last().Seats.Single().SeatType);
            Assert.AreEqual(-5, this._sut.Events.OfType<AvailableSeatsChanged>().Last().Seats.Single().Quantity);
            Assert.AreEqual(5, this._sut.Events.OfType<SeatsReserved>().Single().ReservationDetails.ElementAt(0).Quantity);
        }

        [Test]
        public void When_reserving_less_seats_than_total_then_reserves_all_requested_seats() {
            this._sut.MakeReservation(Guid.NewGuid(), new []{new SeatQuantity(SeatTypeId, 4) });
            
            Assert.AreEqual(SeatTypeId, this._sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).SeatType);
            Assert.AreEqual(4, this._sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).Quantity);
        }

        [Test]
        public void When_reserving_less_seats_than_total_then_reduces_remaining_seats() {
            this._sut.MakeReservation(Guid.NewGuid(), new[]{new SeatQuantity(SeatTypeId, 4) });

            Assert.AreEqual(SeatTypeId, this._sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).SeatType);
            Assert.AreEqual(-4, this._sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).Quantity);
        }

        [Test]
        public void When_reserving_more_seats_than_total_then_reserves_total() {
            var id = Guid.NewGuid();
            _sut.MakeReservation(id, new[] {new SeatQuantity(SeatTypeId, 11)});

            Assert.AreEqual(SeatTypeId, this._sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).SeatType);
            Assert.AreEqual(10, this._sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).Quantity);
        }

        [Test]
        public void When_reserving_more_seats_than_total_then_reduces_remaining_seats() {
            var id = Guid.NewGuid();
            _sut.MakeReservation(id, new[] { new SeatQuantity(SeatTypeId, 11) });

            Assert.AreEqual(SeatTypeId, this._sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).SeatType);
            Assert.AreEqual(-10, this._sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).Quantity);
        }

        [Test]
        public void When_reserving_non_existing_seat_type_then_throws() {
            var id = Guid.NewGuid();
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.MakeReservation(id, new[] {
                new SeatQuantity(SeatTypeId, 11),
                new SeatQuantity(Guid.NewGuid(), 3)
            }));
        }
    }
}