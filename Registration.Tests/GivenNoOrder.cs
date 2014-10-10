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
    public class GivenNoOrder {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();
        private static readonly OrderTotal OrderTotal = new OrderTotal() {
            Total = 33, 
            Lines = new []{new OrderLine()}
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
        }

        [Test]
        public void When_creating_order_then_is_placed_with_specified_id() {
            this._sut.When(new RegisterToConference {
                ConferenceId = ConferenceId,
                OrderId = OrderId,
                Seats = new[] { new SeatQuantity(SeatTypeId, 5)}
            });

            Assert.AreEqual(OrderId, this._sut.ThenHasOne<OrderPlaced>().SourceId);
        }

        [Test]
        public void When_placing_order_then_has_full_details() {
            this._sut.When(new RegisterToConference {
                ConferenceId = ConferenceId,
                OrderId = OrderId,
                Seats = new[] { new SeatQuantity(SeatTypeId, 5) }
            });

            var @event = this._sut.ThenHasOne<OrderPlaced>();

            Assert.AreEqual(OrderId, @event.SourceId);
            Assert.AreEqual(ConferenceId, @event.ConferenceId);
            Assert.AreEqual(1, @event.Seats.Count());
            Assert.AreEqual(5, @event.Seats.ElementAt(0).Quantity);
        }

        [Test]
        public void When_placing_order_then_has_access_code() {
            this._sut.When(new RegisterToConference {
                ConferenceId = ConferenceId,
                OrderId = OrderId,
                Seats = new[] { new SeatQuantity(SeatTypeId, 5) }
            });

            var @event = this._sut.ThenHasOne<OrderPlaced>();
            Assert.IsNotEmpty(@event.AccessCode);
        }

        [Test]
        public void When_placing_order_then_defines_expected_expiration_time_in_15_minutes() {
            this._sut.When(new RegisterToConference {
                ConferenceId = ConferenceId,
                OrderId = OrderId,
                Seats = new[] { new SeatQuantity(SeatTypeId, 5) }
            });

            var @event = this._sut.ThenHasOne<OrderPlaced>();
            var relativeExpiration = @event.ReservationAutoExpiration.Subtract(DateTime.UtcNow);
            Assert.IsTrue(relativeExpiration.Minutes<=16);
            Assert.IsTrue(relativeExpiration.Minutes>=14);
        }

        [Test]
        public void When_creating_order_then_calculates_totals() {
            this._sut.When(new RegisterToConference {
                ConferenceId = ConferenceId,
                OrderId = OrderId,
                Seats = new[] { new SeatQuantity(SeatTypeId, 5) }
            });

            var totals = this._sut.ThenHasOne<OrderTotalsCalculated>();

            Assert.AreEqual(OrderTotal.Total, totals.Total);
            Assert.AreEqual(OrderTotal.Lines.Count, totals.Lines.Length);
            Assert.AreEqual(OrderTotal.Lines.First().LineTotal, totals.Lines[0].LineTotal);
        }
    }
}