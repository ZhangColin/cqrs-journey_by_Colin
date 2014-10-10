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
    public class GivenPlacedOrder {
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
                Seats = new[] { new SeatQuantity(SeatTypeId, 5)},
                ReservationAutoExpiration = DateTime.UtcNow
            });
        }

        [Test]
        public void When_updating_seats_then_updates_order_with_new_seats() {
            this._sut.When(new RegisterToConference() {
                ConferenceId = ConferenceId,
                OrderId = OrderId,
                Seats = new[] { new SeatQuantity(SeatTypeId, 20) }
            });

            var @event = this._sut.ThenHasOne<OrderUpdated>();
            Assert.AreEqual(OrderId, @event.SourceId);
            Assert.AreEqual(1, @event.Seats.Count());
            Assert.AreEqual(20, @event.Seats.ElementAt(0).Quantity);
        }

        [Test]
        public void When_marking_a_subset_of_seats_as_reserved_then_order_is_partially_reserved() {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this._sut.When(new MarkSeatsAsReserved {
                OrderId = OrderId,
                Expiration = expiration,
                Seats = new List<SeatQuantity>{ new SeatQuantity(SeatTypeId, 3) }
            });

            var @event = this._sut.ThenHasOne<OrderPartiallyReserved>();
            Assert.AreEqual(OrderId, @event.SourceId);
            Assert.AreEqual(1, @event.Seats.Count());
            Assert.AreEqual(3, @event.Seats.ElementAt(0).Quantity);
            Assert.AreEqual(expiration, @event.ReservationExpiration);
        }

        [Test]
        public void When_marking_a_subset_of_seats_as_reserved_then_totals_are_calculated() {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this._sut.When(new MarkSeatsAsReserved {
                OrderId = OrderId,
                Expiration = expiration,
                Seats = new List<SeatQuantity> { new SeatQuantity(SeatTypeId, 3)}
            });

            var @event = this._sut.ThenHasOne<OrderTotalsCalculated>();
            Assert.AreEqual(OrderId, @event.SourceId);
            Assert.AreEqual(33, @event.Total);
            Assert.AreEqual(1, @event.Lines.Count());
            Assert.AreSame(OrderTotal.Lines.Single(), @event.Lines.Single());

            this._pricingService.Verify(s => s.CalculateTotal(ConferenceId,
                It.Is<ICollection<SeatQuantity>>(x => x.Single().SeatType == SeatTypeId && x.Single().Quantity == 3)));
        }

        [Test]
        public void When_marking_all_seats_as_reserved_then_order_is_reserved() {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this._sut.When(new MarkSeatsAsReserved() {
                OrderId = OrderId,
                Expiration = expiration,
                Seats = new List<SeatQuantity>() { new SeatQuantity(SeatTypeId, 5)}
            });

            var @event = this._sut.ThenHasOne<OrderReservationCompleted>();
            Assert.AreEqual(OrderId, @event.SourceId);
            Assert.AreEqual(1, @event.Seats.Count());
            Assert.AreEqual(5, @event.Seats.ElementAt(0).Quantity);
            Assert.AreEqual(expiration, @event.ReservationExpiration);
        }

        [Test]
        public void When_marking_all_as_reserved_then_totals_are_not_recalculated() {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this._sut.When(new MarkSeatsAsReserved() {
                OrderId = OrderId, 
                Expiration = expiration,
                Seats = new List<SeatQuantity> { new SeatQuantity(SeatTypeId, 5)}
            });

            Assert.AreEqual(0, this._sut.Events.OfType<OrderTotalsCalculated>().Count());
        }

        [Test]
        public void When_expiring_order_then_notifies() {
            this._sut.When(new RejectOrder() {OrderId = OrderId});

            var @event = this._sut.ThenHasSingle<OrderExpired>();
            Assert.AreEqual(OrderId, @event.SourceId);
        }

        [Test]
        public void When_assigning_registrant_information_then_raises_integration_event() {
            this._sut.When(new AssignRegistrantDetails() {
                OrderId = OrderId,
                FirstName = "foo",
                LastName = "bar",
                Email = "foo@bar.com"
            });

            var @event = this._sut.ThenHasSingle<OrderRegistrantAssigned>();
            Assert.AreEqual(OrderId, @event.SourceId);
            Assert.AreEqual("foo", @event.FirstName);
            Assert.AreEqual("bar", @event.LastName);
            Assert.AreEqual("foo@bar.com", @event.Email);
        }
    }
}