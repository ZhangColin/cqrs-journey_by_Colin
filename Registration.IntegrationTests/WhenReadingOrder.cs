using System;
using Conference.Contracts;
using Infrastructure.Serialization;
using NUnit.Framework;
using Registration.Contracts.Events;
using Registration.Handlers;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Registration.IntegrationTests {
    [TestFixture]
    public class WhenReadingOrder {
        private string _dbName;

        private PricedOrderViewModelGenerator _sut;
        private IOrderDao _dao;

        private SeatCreated[] _seatCreatedEvents;

        private Guid _orderId = Guid.NewGuid();
        private OrderPlaced _orderPlaced;

        private PricedOrder _dto;

        [SetUp]
        public void Setup() {
            this._dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            var blobStorage = new MemoryBlobStorage();
            this._sut = new PricedOrderViewModelGenerator(() => new ConferenceRegistrationDbContext(_dbName));
            this._dao = new OrderDao(() => new ConferenceRegistrationDbContext(_dbName), blobStorage,
                new JsonTextSerializer());

            this._seatCreatedEvents = new[]
                                     {
                                         new SeatCreated { SourceId = Guid.NewGuid(), Name = "General" },
                                         new SeatCreated { SourceId = Guid.NewGuid(), Name = "Precon" }
                                     };
            this._sut.Handle(this._seatCreatedEvents[0]);
            this._sut.Handle(this._seatCreatedEvents[1]);

            this._orderPlaced = new OrderPlaced {
                SourceId = _orderId,
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10),
                Version = 2,
            };

            this._sut.Handle(_orderPlaced);

            this._dto = this._dao.FindPricedOrder(_orderId);
        }

        [TearDown]
        public void Dispose() {
            using (var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if (context.Database.Exists())
                    context.Database.Delete();
            }
        }

        [Test]
        public void then_creates_model_with_expiration_date_and_version() {
            Assert.NotNull(_dto);
            Assert.IsTrue(_dto.ReservationExpirationDate.Value >= _orderPlaced.ReservationAutoExpiration.AddSeconds(-1)
                && _dto.ReservationExpirationDate.Value <= _orderPlaced.ReservationAutoExpiration.AddSeconds(1));
            Assert.AreEqual(_orderPlaced.Version, _dto.OrderVersion);
        }

        [Test]
        public void then_order_placed_is_idempotent() {
            this._sut.Handle(_orderPlaced);

            _dto = this._dao.FindPricedOrder(_orderId);

            Assert.NotNull(_dto);
            Assert.IsTrue(_dto.ReservationExpirationDate.Value >= _orderPlaced.ReservationAutoExpiration.AddSeconds(-1)
                && _dto.ReservationExpirationDate.Value <= _orderPlaced.ReservationAutoExpiration.AddSeconds(1));
            Assert.AreEqual(_orderPlaced.Version, _dto.OrderVersion);
        }
    }
}