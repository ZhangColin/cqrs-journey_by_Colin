using System;
using System.Linq;
using Infrastructure.Messaging;
using NUnit.Framework;
using Registration.Commands;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Events;

namespace Registration.Tests.Process {
    [TestFixture]
    public class WhenReservationConfirmationIsReceivedForNonCurrentCorrelationId {
        private RegistrationProcessManager _sut;
        private Guid _orderId;
        private Guid _conferenceId;
        private int _initialCommandCount;

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

            var makeReservationCommand = this._sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            var seatsReserved = new SeatsReserved() {
                SourceId = this._conferenceId,
                ReservationId = makeReservationCommand.ReservationId,
                ReservationDetails = new SeatQuantity[0]
            };

            this._initialCommandCount = this._sut.Commands.Count();

            this._sut.Handle(new Envelope<SeatsReserved>(seatsReserved) {
                CorrelationId = Guid.NewGuid().ToString()
            });
        }

        [Test]
        public void Then_does_not_update_order_status() {
            Assert.AreEqual(this._initialCommandCount, this._sut.Commands.Count());
        }

        [Test]
        public void Then_does_not_transition_state() {
            Assert.AreEqual(RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation, _sut.State);
        }
    }
}