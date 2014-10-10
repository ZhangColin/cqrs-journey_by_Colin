using System;
using System.Linq;
using Infrastructure.Serialization;
using NUnit.Framework;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Handlers;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Registration.IntegrationTests {
    [TestFixture]
    public class GivenAPlacedOrder {
        private string _dbName;
        private DraftOrderViewModelGenerator _sut;
        private IOrderDao _dao;

        private OrderPlaced _orderPlacedEvent;

        [SetUp]
        public void Setup() {
            this._dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            var blobStorage = new MemoryBlobStorage();
            this._sut = new DraftOrderViewModelGenerator(() => new ConferenceRegistrationDbContext(_dbName));
            this._dao = new OrderDao(() => new ConferenceRegistrationDbContext(_dbName), blobStorage,
                new JsonTextSerializer());

            System.Diagnostics.Trace.Listeners.Clear();

            this._orderPlacedEvent = new OrderPlaced {
                SourceId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                AccessCode = "asdf",
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 5) },
                Version = 1
            };

            _sut.Handle(_orderPlacedEvent);
        }

        [TearDown]
        public void Dispose() {
            using (var context = new ConferenceRegistrationDbContext(this._dbName)) {
                if (context.Database.Exists())
                    context.Database.Delete();
            }
        }

        [Test]
        public void Then_read_model_created() {
            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.NotNull(dto);
            Assert.AreEqual("asdf", dto.AccessCode);
            Assert.AreEqual(_orderPlacedEvent.ConferenceId, dto.ConferenceId);
            Assert.AreEqual(_orderPlacedEvent.SourceId, dto.OrderId);
            Assert.AreEqual(1, dto.Lines.Count);
            Assert.AreEqual(1, dto.OrderVersion);
        }

        [Test]
        public void Then_order_is_pending_reservation() {
            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(DraftOrder.States.PendingReservation, dto.State);
        }

        [Test]
        public void Then_order_does_not_contain_expiration_yet() {
            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.Null(dto.ReservationExpirationDate);
        }

        [Test]
        public void Then_one_order_line_per_seat_type_is_created() {
            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(1, dto.Lines.Count);
            Assert.AreEqual(_orderPlacedEvent.Seats.First().SeatType, dto.Lines.First().SeatType);
        }

        [Test]
        public void Then_order_line_seats_are_requested_but_not_reserved_yet() {
            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(_orderPlacedEvent.Seats.First().Quantity, dto.Lines.First().RequestedSeats);
            Assert.AreEqual(0, dto.Lines.First().ReservedSeats);
        }

        [Test]
        public void When_registrant_information_assigned_Then_email_is_persisted() {
            _sut.Handle(new OrderRegistrantAssigned {
                Email = "a@b.com",
                FirstName = "A",
                LastName = "Z",
                SourceId = _orderPlacedEvent.SourceId,
                Version = 5
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual("a@b.com", dto.RegistrantEmail);
        }

        [Test]
        public void When_registrant_information_assigned_Then_can_locate_order() {
            _sut.Handle(new OrderRegistrantAssigned {
                Email = "a@b.com",
                FirstName = "A",
                LastName = "Z",
                SourceId = _orderPlacedEvent.SourceId,
                Version = 5
            });

            var actual =_dao.LocateOrder("a@b.com", _orderPlacedEvent.AccessCode);

            Assert.NotNull(actual);
            Assert.AreEqual(_orderPlacedEvent.SourceId, actual.Value);
        }

        [Test]
        public void When_order_is_updated_Then_removes_original_lines() {
            _sut.Handle(new OrderUpdated {
                SourceId = _orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                Version = 2
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.False(dto.Lines.Any(line => line.SeatType == _orderPlacedEvent.Seats.First().SeatType));
        }

        [Test]
        public void When_order_is_updated_Then_adds_new_lines() {
            var newSeat = Guid.NewGuid();
            _sut.Handle(new OrderUpdated {
                SourceId = _orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(newSeat, 2) },
                Version = 4,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(2, dto.Lines.First().RequestedSeats);
            Assert.AreEqual(newSeat, dto.Lines.First().SeatType);
            Assert.AreEqual(4, dto.OrderVersion);
        }

        [Test]
        public void When_order_updated_event_is_received_twice_Then_no_ops() {
            var newSeat = Guid.NewGuid();
            _sut.Handle(new OrderUpdated {
                SourceId = _orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(newSeat, 2) },
                Version = 4,
            });

            var seatType =_dao.FindDraftOrder(_orderPlacedEvent.SourceId).Lines.First().SeatType;

            _sut.Handle(new OrderUpdated {
                SourceId = _orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(newSeat, 2) },
                Version = 4,
            });

            Assert.AreEqual(seatType, _dao.FindDraftOrder(_orderPlacedEvent.SourceId).Lines.First().SeatType);
        }

        [Test]
        public void When_order_is_updated_Then_state_is_pending_reservation() {
            _sut.Handle(new OrderUpdated {
                SourceId = _orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                Version = 4,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(DraftOrder.States.PendingReservation, dto.State);
        }

        [Test]
        public void When_order_is_updated_Then_removes_original_lines_from_originating_order() {
            var secondOrder = Guid.NewGuid();
            _sut.Handle(new OrderPlaced {
                SourceId = secondOrder,
                ConferenceId = Guid.NewGuid(),
                AccessCode = "asdf",
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 5) },
                Version = 1,
            });

            _sut.Handle(new OrderUpdated {
                SourceId = _orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                Version = 4,
            });

            var dto =_dao.FindDraftOrder(secondOrder);

            Assert.AreEqual(1, dto.Lines.Count);
        }

        [Test]
        public void When_order_partially_reserved_Then_sets_order_expiration() {
            _sut.Handle(new OrderPartiallyReserved {
                SourceId = _orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                // We got two rather than the requested 5.
                Seats = new[] { new SeatQuantity(_orderPlacedEvent.Seats.First().SeatType, 2) },
                Version = 3,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.NotNull(dto.ReservationExpirationDate);
        }

        [Test]
        public void When_order_partially_reserved_Then_updates_reserved_seats() {
            _sut.Handle(new OrderPartiallyReserved {
                SourceId = _orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                // We got two rather than the requested 5.
                Seats = new[] { new SeatQuantity(_orderPlacedEvent.Seats.First().SeatType, 2) },
                Version = 3,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(2, dto.Lines.First().ReservedSeats);
        }

        [Test]
        public void When_order_partially_reserved_Then_state_is_partially_reserved() {
            _sut.Handle(new OrderPartiallyReserved {
                SourceId = _orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                // We got two rather than the requested 5.
                Seats = new[] { new SeatQuantity(_orderPlacedEvent.Seats.First().SeatType, 2) },
                Version = 3,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(DraftOrder.States.PartiallyReserved, dto.State);
            Assert.AreEqual(3, dto.OrderVersion);
        }

        [Test]
        public void When_order_fully_reserved_Then_sets_order_expiration() {
            _sut.Handle(new OrderReservationCompleted {
                SourceId = _orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                Seats = new[] { _orderPlacedEvent.Seats.First() },
                Version = 3,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.NotNull(dto.ReservationExpirationDate);
        }

        [Test]
        public void When_order_fully_reserved_Then_updates_reserved_seats() {
            _sut.Handle(new OrderReservationCompleted {
                SourceId = _orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                Seats = new[] { _orderPlacedEvent.Seats.First() },
                Version = 3,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(dto.Lines.First().RequestedSeats, dto.Lines.First().ReservedSeats);
        }

        [Test]
        public void When_order_fully_reserved_Then_state_is_reservation_completed() {
            _sut.Handle(new OrderReservationCompleted {
                SourceId = _orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                Seats = new[] { _orderPlacedEvent.Seats.First() },
                Version = 3,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(DraftOrder.States.ReservationCompleted, dto.State);
            Assert.AreEqual(3, dto.OrderVersion);
        }

        [Test]
        public void When_order_confirmed_v1_Then_order_state_is_confirmed() {
            _sut.Handle(new OrderPaymentConfirmed {
                SourceId = _orderPlacedEvent.SourceId,
                Version = 7,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(DraftOrder.States.Confirmed, dto.State);
            Assert.AreEqual(7, dto.OrderVersion);
        }

        [Test]
        public void When_order_confirmed_Then_order_state_is_confirmed() {
            _sut.Handle(new OrderConfirmed {
                SourceId = _orderPlacedEvent.SourceId,
                Version = 7,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(DraftOrder.States.Confirmed, dto.State);
        }

        [Test]
        public void When_order_confirmed_Then_updates_order_version() {
            _sut.Handle(new OrderConfirmed {
                SourceId = _orderPlacedEvent.SourceId,
                Version = 7,
            });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);

            Assert.AreEqual(7, dto.OrderVersion);
        }

        [Test]
        public void When_order_confirmed_for_older_version_Then_no_ops() {
            _sut.Handle(new OrderUpdated {
                SourceId = _orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                Version = 4,
            });

            _sut.Handle(new OrderConfirmed { SourceId = _orderPlacedEvent.SourceId, Version = 1, });

            var dto =_dao.FindDraftOrder(_orderPlacedEvent.SourceId);
            Assert.AreEqual(4, dto.OrderVersion);
            Assert.AreEqual(DraftOrder.States.PendingReservation, dto.State);
        }
    }
}