using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Infrastructure.Messaging;
using NUnit.Framework;
using Registration.Commands;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Events;

namespace Registration.Tests.Process {
    [TestFixture]
    public class WhenOrderUpdateIsReceived {
        private RegistrationProcessManager _sut;
        private Guid _orderId;
        private Guid _conferenceId;

        private OrderUpdated _orderUpdated;

        [SetUp]
        public void Setup() {
            this._sut = new RegistrationProcessManager();
            this._orderId = Guid.NewGuid();
            this._conferenceId = Guid.NewGuid();

            this._sut.Handle(new OrderPlaced() {
                SourceId = this._orderId,
                ConferenceId = this._conferenceId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(22))
            });

            this._orderUpdated = new OrderUpdated() {
                SourceId = Guid.NewGuid(),
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 3)}
            };

            _sut.Handle(this._orderUpdated);
        }

        [Test]
        public void Then_sends_new_reservation_command() {
            Assert.AreEqual(2, this._sut.Commands.Select(x=>x.Body).OfType<MakeSeatReservation>().Count());
        }

        [Test]
        public void Then_reservation_is_requested_for_specific_conference() {
            var newReservation = this._sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().ElementAt(1);

            Assert.AreEqual(this._conferenceId, newReservation.ConferenceId);
            Assert.AreEqual(3, newReservation.Seats[0].Quantity);
        }

        [Test]
        public void Then_saves_reservation_command_id_for_later_user() {
            var reservation = _sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().ElementAt(1);

            Assert.AreEqual(reservation.Id, this._sut.SeatReservationCommandId);
        }

        [Test]
        public void Then_transitions_to_awaiting_reservation_confirmation_state() {
            Assert.AreEqual(RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation, _sut.State);
        }
    }
}