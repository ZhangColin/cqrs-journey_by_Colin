using System;
using System.Linq;
using Conference.Common.Utils;
using Moq;
using NUnit.Framework;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Handlers;

namespace Registration.Tests {
    [TestFixture]
    public class GivenAPaidOrder {
        private Guid _orderId = Guid.NewGuid();
        private EventSourcingTestHelper<SeatAssignments> _seatHelper;
        private EventSourcingTestHelper<Order> _orderHelper;

        [SetUp]
        public void Setup() {
            this._seatHelper = new EventSourcingTestHelper<SeatAssignments>();
            this._orderHelper = new EventSourcingTestHelper<Order>();

            this._orderHelper.Setup(new OrderCommandHandler(this._orderHelper.Repository, Mock.Of<IPricingService>()));
            this._orderHelper.Given(new OrderPlaced() {
                SourceId = _orderId,
                ConferenceId = Guid.NewGuid(),
                Seats = new[] {
                    new SeatQuantity(Guid.NewGuid(), 5),
                    new SeatQuantity(Guid.NewGuid(), 10)
                },
                ReservationAutoExpiration = DateTime.UtcNow.AddDays(1),
                AccessCode = HandleGenerator.Generate(6)
            }, new OrderPaymentConfirmed() {SourceId = _orderId});

            this._seatHelper.Setup(new SeatAssignmentsHandler(this._orderHelper.Repository, this._seatHelper.Repository));
        }

        [Test]
        public void When_order_confirmed_then_seats_assignments_created() {
            _seatHelper.When(new OrderConfirmed() {SourceId = _orderId});

            var @event = _seatHelper.ThenHasSingle<SeatAssignmentsCreated>();

            Assert.AreNotEqual(_orderId, @event.SourceId);
            Assert.AreEqual(_orderId, @event.OrderId);
            Assert.AreEqual(15, @event.Seats.Count());
        }
    }
}