using System;
using System.Linq;
using Infrastructure.Messaging;
using NUnit.Framework;
using Payments.Contracts.Events;
using Registration.Commands;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Events;

namespace Registration.Tests.Process {
    [TestFixture]
    public class WhenPaymentConfirmationIsReceived {
        private RegistrationProcessManager _sut;
        private Guid _orderId;
        private Guid _conferenceId;
        private Guid _reservationId;

        [SetUp]
        public void Setup() {
            this._sut = new RegistrationProcessManager();
            this._orderId = Guid.NewGuid();
            this._conferenceId = Guid.NewGuid();

            var seatType = Guid.NewGuid();

            this._sut.Handle(
                new OrderPlaced {
                    SourceId = this._orderId,
                    ConferenceId = this._conferenceId,
                    Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                    ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(22))
                });

            var makeReservationCommand = this._sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            this._reservationId = makeReservationCommand.ReservationId;

            this._sut.Handle(
                new Envelope<SeatsReserved>(
                    new SeatsReserved {
                        SourceId = this._conferenceId,
                        ReservationId = makeReservationCommand.ReservationId,
                        ReservationDetails = new[] { new SeatQuantity(seatType, 2) }
                    }) {
                        CorrelationId = makeReservationCommand.Id.ToString()
                    });

            _sut.Handle(new PaymentCompleted() {
                PaymentSourceId = this._orderId
            });
        }

        [Test]
        public void Then_confirms_order() {
            var command = _sut.Commands.Select(x => x.Body).OfType<ConfirmOrder>().Single();

            Assert.AreEqual(this._orderId, command.OrderId);
        }

        [Test]
        public void Then_transitions_state() {
            Assert.AreEqual(RegistrationProcessManager.ProcessState.PaymentConfirmationReceived, this._sut.State);
        }
    }
}