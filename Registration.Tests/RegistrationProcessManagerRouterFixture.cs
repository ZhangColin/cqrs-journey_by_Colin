using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Infrastructure.Messaging;
using Infrastructure.Processes;
using NUnit.Framework;
using Payments.Contracts.Events;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.Events;

namespace Registration.Tests {
    [TestFixture]
    public class RegistrationProcessManagerRouterFixture {
        [Test]
        public void When_order_placed_then_routes_and_saves() {
            var context = new StubProcessManagerDataContext<RegistrationProcessManager>();
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new OrderPlaced { SourceId = Guid.NewGuid(), ConferenceId = Guid.NewGuid(), Seats = new SeatQuantity[0] });

            Assert.AreEqual(1, context.SavedProcesses.Count);
            Assert.IsTrue(context.DisposeCalled);
        }

        [Test]
        public void When_order_placed_is_is_reprocessed_then_routes_and_saves() {
            var pm = new RegistrationProcessManager {
                State = RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation,
                OrderId = Guid.NewGuid(),
                ReservationId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new OrderPlaced { SourceId = pm.OrderId, ConferenceId = pm.ConferenceId, Seats = new SeatQuantity[0] });

            Assert.AreEqual(1, context.SavedProcesses.Count);
            Assert.IsTrue(context.DisposeCalled);
        }

        [Test]
        public void When_order_updated_then_routes_and_saves() {
            var pm = new RegistrationProcessManager {
                State = RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new OrderUpdated { SourceId = pm.OrderId, Seats = new SeatQuantity[0] });

            Assert.AreEqual(1, context.SavedProcesses.Count);
            Assert.IsTrue(context.DisposeCalled);
        }

        [Test]
        public void When_reservation_accepted_then_routes_and_saves() {
            var pm = new RegistrationProcessManager {
                State = RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                SeatReservationCommandId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(
                new Envelope<SeatsReserved>(
                    new SeatsReserved { SourceId = pm.ConferenceId, ReservationId = pm.ReservationId, ReservationDetails = new SeatQuantity[0] }) {
                        CorrelationId = pm.SeatReservationCommandId.ToString()
                    });

            Assert.AreEqual(1, context.SavedProcesses.Count);
            Assert.IsTrue(context.DisposeCalled);
        }

        [Test]
        public void When_payment_received_then_routes_and_saves() {
            var pm = new RegistrationProcessManager {
                State = RegistrationProcessManager.ProcessState.ReservationConfirmationReceived,
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10),
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new PaymentCompleted { PaymentSourceId = pm.OrderId });

            Assert.AreEqual(1, context.SavedProcesses.Count);
            Assert.IsTrue(context.DisposeCalled);
        }

        [Test]
        public void When_order_confirmed_received_then_routes_and_saves() {
            var pm = new RegistrationProcessManager {
                State = RegistrationProcessManager.ProcessState.PaymentConfirmationReceived,
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10),
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new OrderConfirmed { SourceId = pm.OrderId });

            Assert.AreEqual(1, context.SavedProcesses.Count);
            Assert.IsTrue(context.DisposeCalled);
        }

        [Test]
        public void When_order_expired_then_routes_and_saves() {
            var pm = new RegistrationProcessManager {
                State = RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };

            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new Commands.ExpireRegistrationProcess { ProcessId = pm.Id });

            Assert.AreEqual(1, context.SavedProcesses.Count);
            Assert.IsTrue(context.DisposeCalled);
        }
    }

    class StubProcessManagerDataContext<T> : IProcessManagerDataContext<T> where T : class, IProcessManager {
        public readonly List<T> SavedProcesses = new List<T>();

        public readonly List<T> Store = new List<T>();

        public bool DisposeCalled { get; set; }

        public T Find(Guid id) {
            return this.Store.SingleOrDefault(x => x.Id == id);
        }

        public void Save(T processManager) {
            this.SavedProcesses.Add(processManager);
        }

        public T Find(Expression<Func<T, bool>> predicate, bool includeCompleted = false) {
            return this.Store.AsQueryable().Where(x => includeCompleted || !x.Completed).SingleOrDefault(predicate);
        }

        public void Dispose() {
            this.DisposeCalled = true;
        }
    }
}