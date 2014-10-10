﻿using System;
using System.Linq;
using Infrastructure.Messaging;
using NUnit.Framework;
using Registration.Commands;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Events;

namespace Registration.Tests.Process {
    [TestFixture]
    public class WhenReservationIsExpired {
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
                    Seats = new[] {new SeatQuantity(Guid.NewGuid(), 2)},
                    ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(22))
                });

            var makeReservationCommand = this._sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            this._reservationId = makeReservationCommand.ReservationId;

            this._sut.Handle(
                new Envelope<SeatsReserved>(
                    new SeatsReserved {
                        SourceId = this._conferenceId,
                        ReservationId = makeReservationCommand.ReservationId,
                        ReservationDetails = new[] {new SeatQuantity(seatType, 2)}
                    }) {
                        CorrelationId = makeReservationCommand.Id.ToString()
                    });
        
            var expirationCommand = _sut.Commands.Select(x => x.Body).OfType<ExpireRegistrationProcess>().Single();
            _sut.Handle(expirationCommand);
        }

        [Test]
        public void Then_cancels_seat_reservation()
        {
            var command = _sut.Commands.Select(x => x.Body).OfType<CancelSeatReservation>().Single();

            Assert.AreEqual(this._reservationId, command.ReservationId);
            Assert.AreEqual(this._conferenceId, command.ConferenceId);
        }

        [Test]
        public void Then_updates_order_status()
        {
            var command = _sut.Commands.Select(x => x.Body).OfType<RejectOrder>().Single();

            Assert.AreEqual(this._orderId, command.OrderId);
        }

        [Test]
        public void Then_transitions_state()
        {
            Assert.AreEqual(true, _sut.Completed);
        }
    }
}