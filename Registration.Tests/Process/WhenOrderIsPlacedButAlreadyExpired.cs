using System;
using System.Linq;
using NUnit.Framework;
using Registration.Commands;
using Registration.Contracts;
using Registration.Contracts.Events;

namespace Registration.Tests.Process {
    [TestFixture]
    public class WhenOrderIsPlacedButAlreadyExpired {
        private RegistrationProcessManager _sut;
        private OrderPlaced _orderPlaced;

        [SetUp]
        public void Setup() {
            this._sut = new RegistrationProcessManager();

            this._orderPlaced = new OrderPlaced() {
                SourceId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(-1))
            };

            this._sut.Handle(_orderPlaced);
        }

        [Test]
        public void Then_order_is_rejected() {
            var command = this._sut.Commands.Select(x => x.Body).Cast<RejectOrder>().Single();

            Assert.AreEqual(_orderPlaced.SourceId, command.OrderId);
        }

        [Test]
        public void Then_process_manager_is_completed() {
            Assert.IsTrue(_sut.Completed);
        }
    }
}