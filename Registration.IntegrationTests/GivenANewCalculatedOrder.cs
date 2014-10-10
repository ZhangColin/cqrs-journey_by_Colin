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
    public class GivenANewCalculatedOrder {
        private string _dbName;

        private PricedOrderViewModelGenerator _sut;
        private IOrderDao _dao;

        private SeatCreated[] _seatCreatedEvents;

        private SeatUpdated[] _seatUpdatedEvents;

        private Guid _orderId = Guid.NewGuid();

        private PricedOrder _dto;

        [SetUp]
        public void Setup() {
            this._dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using(var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if(context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            var blobStorage = new MemoryBlobStorage();
            this._sut = new PricedOrderViewModelGenerator(() => new ConferenceRegistrationDbContext(_dbName));
            this._dao = new OrderDao(() => new ConferenceRegistrationDbContext(_dbName), blobStorage,
                new JsonTextSerializer());

            this._seatCreatedEvents = new[] {
                new SeatCreated {SourceId = Guid.NewGuid(), Name = "General"},
                new SeatCreated {SourceId = Guid.NewGuid(), Name = "Precon"}
            };
            this._sut.Handle(this._seatCreatedEvents[0]);
            this._sut.Handle(this._seatCreatedEvents[1]);

            this._seatUpdatedEvents = new[] {
                new SeatUpdated {SourceId = _seatCreatedEvents[0].SourceId, Name = "General_Updated"},
                new SeatUpdated {SourceId = _seatCreatedEvents[1].SourceId, Name = "Precon_Updated"},
            };
            this._sut.Handle(this._seatUpdatedEvents[0]);

            this._sut.Handle(new OrderPlaced {
                SourceId = _orderId,
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10),
                Version = 2,
            });
            this._sut.Handle(new OrderTotalsCalculated {
                SourceId = _orderId,
                Lines = new[] {
                    new SeatOrderLine {
                        LineTotal = 50,
                        SeatType = this._seatCreatedEvents[0].SourceId,
                        Quantity = 10,
                        UnitPrice = 5
                    },
                    new SeatOrderLine {
                        LineTotal = 10,
                        SeatType = this._seatCreatedEvents[1].SourceId,
                        Quantity = 1,
                        UnitPrice = 10
                    },
                },
                Total = 60,
                IsFreeOfCharge = true,
                Version = 9,
            });

            this._dto = this._dao.FindPricedOrder(_orderId);
        }

        [TearDown]
        public void Dispose() {
            using(var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if(context.Database.Exists())
                    context.Database.Delete();
            }
        }

        [Test]
        public void Then_populates_with_updated_description() {
            Assert.Contains("General_Updated", _dto.Lines.Select(x => x.Description).ToList());
            Assert.Contains("Precon", _dto.Lines.Select(x => x.Description).ToList());
        }

        [Test]
        public void When_recalculated__after_new_update_then_replaces_line() {
            this._sut.Handle(_seatUpdatedEvents[1]);
            this._sut.Handle(new OrderTotalsCalculated {
                SourceId = _orderId,
                Lines = new[] {
                    new SeatOrderLine {
                        LineTotal = 10,
                        SeatType = this._seatCreatedEvents[0].SourceId,
                        Quantity = 2,
                        UnitPrice = 5
                    },
                    new SeatOrderLine {
                        LineTotal = 20,
                        SeatType = this._seatCreatedEvents[1].SourceId,
                        Quantity = 2,
                        UnitPrice = 10
                    },
                },
                Total = 30,
                Version = 12,
            });

            var dto = this._dao.FindPricedOrder(_orderId);

            Assert.Contains("General_Updated", dto.Lines.Select(x => x.Description).ToList());
            Assert.Contains("Precon_Updated", dto.Lines.Select(x => x.Description).ToList());
        }
    }
}