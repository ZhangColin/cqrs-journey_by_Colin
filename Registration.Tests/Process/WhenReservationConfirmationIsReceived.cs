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
    public class WhenReservationConfirmationIsReceived {
        private RegistrationProcessManager _sut;
        private Guid _orderId;
        private Guid _conferenceId;

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

            this._sut.Handle(new Envelope<SeatsReserved>(seatsReserved) {
                CorrelationId = makeReservationCommand.Id.ToString()
            });
        }

        [Test]
        public void Then_updates_order_status() {
            var command = _sut.Commands.Select(x => x.Body).OfType<MarkSeatsAsReserved>().Single();
            Assert.AreEqual(this._orderId, command.OrderId);
        }

        [Test]
        public void Then_transitions_state() {
            Assert.AreEqual(RegistrationProcessManager.ProcessState.ReservationConfirmationReceived, _sut.State);
        }
    }
}