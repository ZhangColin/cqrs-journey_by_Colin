using System;
using System.Data.Entity.Infrastructure;
using NUnit.Framework;
using Registration.Database;

namespace Registration.IntegrationTests {
    [TestFixture]
    public class RegistrationProcessManagerDbContextFixture {
        [Test]
        public void when_saving_process_then_can_retrieve_it() {
            var dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new RegistrationProcessManagerDbContext(dbName)) {
                context.Database.Create();
            }

            try {
                Guid id;
                using (var context = new RegistrationProcessManagerDbContext(dbName)) {
                    var pm = new RegistrationProcessManager();
                    context.RegistrationProcesses.Add(pm);
                    context.SaveChanges();
                    id = pm.Id;
                }
                using (var context = new RegistrationProcessManagerDbContext(dbName)) {
                    var pm = context.RegistrationProcesses.Find(id);
                    Assert.NotNull(pm);
                }
            }
            finally {
                using (var context = new RegistrationProcessManagerDbContext(dbName)) {
                    context.Database.Delete();
                }
            }
        }

        [Test]
        public void when_saving_process_performs_optimistic_locking() {
            var dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new RegistrationProcessManagerDbContext(dbName)) {
                context.Database.Create();
            }

            try {
                Guid id;
                using (var context = new RegistrationProcessManagerDbContext(dbName)) {
                    var pm = new RegistrationProcessManager();
                    context.RegistrationProcesses.Add(pm);
                    context.SaveChanges();
                    id = pm.Id;
                }

                using (var context = new RegistrationProcessManagerDbContext(dbName)) {
                    var pm = context.RegistrationProcesses.Find(id);

                    pm.State = RegistrationProcessManager.ProcessState.PaymentConfirmationReceived;

                    using (var innerContext = new RegistrationProcessManagerDbContext(dbName)) {
                        var innerProcess = innerContext.RegistrationProcesses.Find(id);

                        innerProcess.State = RegistrationProcessManager.ProcessState.ReservationConfirmationReceived;

                        innerContext.SaveChanges();
                    }

                    Assert.Throws<DbUpdateConcurrencyException>(() => context.SaveChanges());
                }
            }
            finally {
                using (var context = new RegistrationProcessManagerDbContext(dbName)) {
                    context.Database.Delete();
                }
            }
        }
    }
}