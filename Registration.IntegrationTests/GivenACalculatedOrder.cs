using System;
using System.Linq;
using Conference.Contracts;
using Infrastructure.Serialization;
using NUnit.Framework;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Handlers;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Registration.IntegrationTests {
    [TestFixture]
    public class GivenACalculatedOrder {
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

            this._sut.Handle(new OrderTotalsCalculated {
                SourceId = _orderId,
                Lines = new[]
                    {
                        new SeatOrderLine 
                        { 
                            LineTotal = 50, 
                            SeatType = this._seatCreatedEvents[0].SourceId, 
                            Quantity = 10, 
                            UnitPrice = 5 
                        },
                    },
                Total = 50,
                IsFreeOfCharge = true,
                Version = 4,
            });

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
        public void Then_creates_model() {
            Assert.NotNull(_dto);
            Assert.AreEqual(4, _dto.OrderVersion);
        }

        [Test]
        public void Then_creates_order_lines() {
            Assert.AreEqual(1, _dto.Lines.Count);
            Assert.AreEqual(50, _dto.Lines[0].LineTotal);
            Assert.AreEqual(10, _dto.Lines[0].Quantity);
            Assert.AreEqual(5, _dto.Lines[0].UnitPrice);
            Assert.AreEqual(50, _dto.Total);
        }

        [Test]
        public void Then_populates_description() {
            Assert.Contains("General", _dto.Lines.Select(x => x.Description).ToList());
        }

        [Test]
        public void Then_populates_is_free_of_charge() {
            Assert.AreEqual(true, _dto.IsFreeOfCharge);
        }

        [Test]
        public void When_recalculated_Then_replaces_line() {
            this._sut.Handle(new OrderTotalsCalculated {
                SourceId = _orderId,
                Lines = new[]
                    {
                        new SeatOrderLine 
                        { 
                            LineTotal = 20, 
                            SeatType = this._seatCreatedEvents[1].SourceId, 
                            Quantity = 2, 
                            UnitPrice = 10 
                        },
                    },
                Total = 20,
                Version = 8,
            });

            this._dto = this._dao.FindPricedOrder(_orderId);

            Assert.AreEqual(1, _dto.Lines.Count);
            Assert.AreEqual(20, _dto.Lines[0].LineTotal);
            Assert.AreEqual(2, _dto.Lines[0].Quantity);
            Assert.AreEqual(10, _dto.Lines[0].UnitPrice);
            Assert.AreEqual(20, _dto.Total);
            Assert.Contains("Precon", _dto.Lines.Select(x => x.Description).ToList());
            Assert.AreEqual(8, _dto.OrderVersion);
        }

        [Test]
        public void When_expired_Then_deletes_priced_order() {
            this._sut.Handle(new OrderExpired { SourceId = _orderId });

            this._dto = this._dao.FindPricedOrder(_orderId);

            Assert.IsNull(_dto);
        }

        [Test]
        public void Expiration_is_idempotent() {
            this._sut.Handle(new OrderExpired { SourceId = _orderId, Version = 15 });
            this._sut.Handle(new OrderExpired { SourceId = _orderId, Version = 15 });

            this._dto = this._dao.FindPricedOrder(_orderId);

            Assert.IsNull(_dto);
        }

        [Test]
        public void When_seat_assignments_created_Then_updates_order_with_assignments_id() {
            var assignmentsId = Guid.NewGuid();
            this._sut.Handle(new SeatAssignmentsCreated {
                SourceId = assignmentsId,
                OrderId = _orderId,
            });

            this._dto = this._dao.FindPricedOrder(_orderId);

            Assert.AreEqual(assignmentsId, _dto.AssignmentsId);
            Assert.AreEqual(4, _dto.OrderVersion);
        }

        [Test]
        public void When_confirmed_Then_removes_expiration() {
            this._sut.Handle(new OrderConfirmed { SourceId = _orderId, Version = 15 });

            this._dto = this._dao.FindPricedOrder(_orderId);

            Assert.IsNull(_dto.ReservationExpirationDate);
            Assert.AreEqual(15, _dto.OrderVersion);
        }
    }
}