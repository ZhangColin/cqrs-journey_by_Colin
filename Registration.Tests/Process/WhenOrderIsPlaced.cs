using System;
using System.Linq;
using NUnit.Framework;
using Registration.Commands;
using Registration.Contracts;
using Registration.Contracts.Events;

namespace Registration.Tests.Process {
    [TestFixture]
    public class WhenOrderIsPlaced {
        private RegistrationProcessManager _sut;
        private OrderPlaced _orderPlaced;

        [SetUp]
        public void Setup() {
            this._sut = new RegistrationProcessManager();

            this._orderPlaced = new OrderPlaced() {
                SourceId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(22))
            };

            this._sut.Handle(_orderPlaced);
        }

        [Test]
        public void Then_sends_two_commands() {
            Assert.AreEqual(2, this._sut.Commands.Count());
        }

        [Test]
        public void Then_reservation_is_requested_for_specific_conference() {
            var reservation = this._sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().Single();

            Assert.AreEqual(_orderPlaced.ConferenceId, reservation.ConferenceId);
            Assert.AreEqual(2, reservation.Seats[0].Quantity);
        }

        [Test]
        public void Then_saves_reservation_command_id_for_later_use() {
            var expirationCommandEnvelope = this._sut.Commands.Single(e => e.Body is ExpireRegistrationProcess);

            Assert.IsTrue(expirationCommandEnvelope.Delay>TimeSpan.FromMinutes(32));
            Assert.AreEqual(((ExpireRegistrationProcess)expirationCommandEnvelope.Body).ProcessId, this._sut.Id);
            Assert.AreEqual(expirationCommandEnvelope.Body.Id, this._sut.ExpirationCommandId);
        }

        [Test]
        public void Then_reservation_expiration_time_is_stored_for_later_use() {
            Assert.IsTrue(this._sut.ReservationAutoExpiration.HasValue);
            Assert.AreEqual(_orderPlaced.ReservationAutoExpiration, _sut.ReservationAutoExpiration.Value);
        }

        [Test]
        public void Then_transitions_to_awaiting_reservation_confirmation_state() {
            Assert.AreEqual(RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation, _sut.State);
        }
    }
}