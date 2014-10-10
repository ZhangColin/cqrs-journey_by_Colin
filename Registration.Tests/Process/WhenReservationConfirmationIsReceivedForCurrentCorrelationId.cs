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
    public class WhenReservationConfirmationIsReceivedForCurrentCorrelationId {
        private RegistrationProcessManager _sut;
        private Guid _orderId;
        private Guid _conferenceId;
        private Guid _reservationId;

        private int _initialCommandCount;

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
            this._initialCommandCount = this._sut.Commands.Count();
            _sut.Handle(new Envelope<SeatsReserved>(seatsReserved) { CorrelationId = makeReservationCommand1.Id.ToString() });
        }

        [Test]
        public void Then_does_not_send_new_update_to_order() {
            Assert.AreEqual(this._initialCommandCount, this._sut.Commands.Count());
        }

        [Test]
        public void Then_does_not_transition_state() {
            Assert.AreEqual(RegistrationProcessManager.ProcessState.ReservationConfirmationReceived, _sut.State);
        }
    }
}