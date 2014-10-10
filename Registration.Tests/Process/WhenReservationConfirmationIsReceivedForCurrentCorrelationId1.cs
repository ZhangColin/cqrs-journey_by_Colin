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
    public class WhenReservationConfirmationIsReceivedForCurrentCorrelationId1 {
        private RegistrationProcessManager _sut;
        private Guid _orderId;
        private Guid _conferenceId;
        private Guid _reservationId;

        private Exception _exception;

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

            var makeReservationCommand1 = _sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();

            var seatsReserved = new SeatsReserved { SourceId = this._conferenceId, ReservationId = makeReservationCommand.ReservationId, ReservationDetails = new SeatQuantity[0] };

            try {
                _sut.Handle(new Envelope<SeatsReserved>(seatsReserved) { CorrelationId = Guid.NewGuid().ToString() });
            }
            catch (InvalidOperationException e) {
                this._exception = e;
            }
        }

        [Test]
        public void Then_throws() {
            Assert.NotNull(this._exception);
        }
    }
}