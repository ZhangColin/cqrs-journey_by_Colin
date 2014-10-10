using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Registration.Commands;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Handlers;

namespace Registration.Tests {
    [TestFixture]
    public class GivenFullyReservedOrder {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();
        private static readonly OrderTotal OrderTotal = new OrderTotal() {
            Total = 33,
            Lines = new[] { new OrderLine() }
        };

        private EventSourcingTestHelper<Order> _sut;
        private Mock<IPricingService> _pricingService;

        [SetUp]
        public void Setup() {
            this._pricingService = new Mock<IPricingService>();
            this._pricingService.Setup(x => x.CalculateTotal(ConferenceId, It.IsAny<ICollection<SeatQuantity>>()))
                .Returns(OrderTotal);

            this._sut = new EventSourcingTestHelper<Order>();
            this._sut.Setup(new OrderCommandHandler(this._sut.Repository, this._pricingService.Object));

            this._sut.Given(new OrderPlaced {
                SourceId = OrderId,
                ConferenceId = ConferenceId,
                Seats = new[] {new SeatQuantity(SeatTypeId, 5)},
                ReservationAutoExpiration = DateTime.UtcNow
            }, new OrderReservationCompleted() {
                SourceId = OrderId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(5),
                Seats = new[] {new SeatQuantity(SeatTypeId, 5)}
            });
        }

        [Test]
        public void When_expiring_order_then_notifies() {
            this._sut.When(new RejectOrder() { OrderId = OrderId });

            var @event = this._sut.ThenHasSingle<OrderExpired>();
            Assert.AreEqual(OrderId, @event.SourceId);
        }

        [Test]
        public void When_confirming_order_then_notifies() {
            this._sut.When(new ConfirmOrder { OrderId = OrderId });

            var @event = this._sut.ThenHasSingle<OrderConfirmed>();
            Assert.AreEqual(OrderId, @event.SourceId);
        }

        [Test]
        public void When_updating_an_order_then_updates_seats() {
            this._sut.When(new RegisterToConference() {
                OrderId = OrderId,
                Seats = new []{new SeatQuantity(SeatTypeId, 4) }
            });

            var updated = this._sut.ThenHasOne<OrderUpdated>();
            Assert.AreEqual(OrderId, updated.SourceId);
            Assert.AreEqual(SeatTypeId, updated.Seats.First().SeatType);
            Assert.AreEqual(4, updated.Seats.First().Quantity);
        }

        [Test]
        public void When_updating_an_order_then_recalculates() {
            this._sut.When(new RegisterToConference() {
                OrderId = OrderId,
                Seats = new[] { new SeatQuantity(SeatTypeId, 4) }
            });

            var @event = this._sut.ThenHasOne<OrderTotalsCalculated>();
            Assert.AreEqual(OrderId, @event.SourceId);
            Assert.AreEqual(33, @event.Total);
            Assert.AreEqual(1, @event.Lines.Count());
            Assert.AreSame(OrderTotal.Lines.Single(), @event.Lines.Single());
        }

        [Test]
        public void When_rejecting_confirmed_order_then_throws() {
            this._sut.Given(new OrderConfirmed() {SourceId = OrderId});

            Assert.Throws<InvalidOperationException>(() => this._sut.When(new RejectOrder() {OrderId = OrderId}));
        }

        [Test]
        public void When_rejecting_a_payment_confirmed_order_then_throws() {
            this._sut.Given(new OrderPaymentConfirmed() {SourceId = OrderId});

            Assert.Throws<InvalidOperationException>(() => this._sut.When(new RejectOrder() {OrderId = OrderId}));
        }
    }
}