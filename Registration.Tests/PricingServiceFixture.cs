using System;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using Registration.Contracts;
using Registration.ReadModel;

namespace Registration.Tests {
    [TestFixture]
    public class PricingServiceFixture {
        public static readonly Guid ConferenceId = Guid.NewGuid();
        private Mock<IConferenceDao> _dao;
        private PricingService _sut;
        private SeatType[] _seatTypes;

        [SetUp]
        public void Setup() {
            this._dao = new Mock<IConferenceDao>();
            this._seatTypes = new[] {
                new SeatType(Guid.NewGuid(), ConferenceId, "Name1", "Desc1", 15.10m, 999),
                new SeatType(Guid.NewGuid(), ConferenceId, "Name2", "Desc2", 9.987m, 600)
            };
            _dao.Setup(d => d.GetPublishedSeatTypes(ConferenceId)).Returns(_seatTypes);

            this._sut = new PricingService(_dao.Object);
        }

        [Test]
        public void When_passing_valid_seat_types_then_sums_individual_prices() {
            var actual = _sut.CalculateTotal(ConferenceId, new[] {new SeatQuantity(_seatTypes[0].Id, 3)});

            Assert.AreEqual(45.3m, actual.Total);
            Assert.AreEqual(1, actual.Lines.Count);
            Assert.AreEqual(45.3m, actual.Lines.ElementAt(0).LineTotal);
            Assert.AreEqual(15.1m, ((SeatOrderLine)actual.Lines.ElementAt(0)).UnitPrice);
            Assert.AreEqual(_seatTypes[0].Id, ((SeatOrderLine)actual.Lines.ElementAt(0)).SeatType);
            Assert.AreEqual(3, ((SeatOrderLine)actual.Lines.ElementAt(0)).Quantity);

        }

        [Test]
        public void Whne_passing_invalid_seat_types_then_throws() {
            Assert.Throws<InvalidDataException>(
                () => _sut.CalculateTotal(ConferenceId, new[] {new SeatQuantity(Guid.NewGuid(), 3)}));
        }

        [Test]
        public void Rounds_to_near_2_digit_decimal() {
            var actual = _sut.CalculateTotal(ConferenceId, new[] {new SeatQuantity(_seatTypes[1].Id, 1)});

            Assert.AreEqual(9.99m, actual.Total);
            Assert.AreEqual(1, actual.Lines.Count);
            Assert.AreEqual(9.99m, actual.Lines.ElementAt(0).LineTotal);
            Assert.AreEqual(9.987m, ((SeatOrderLine)actual.Lines.ElementAt(0)).UnitPrice);
            Assert.AreEqual(_seatTypes[1].Id, ((SeatOrderLine)actual.Lines.ElementAt(0)).SeatType);
            Assert.AreEqual(1, ((SeatOrderLine)actual.Lines.ElementAt(0)).Quantity);
        }
    }
}