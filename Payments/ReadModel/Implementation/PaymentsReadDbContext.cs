using System;
using System.Data.Entity;
using System.Linq;
using Payments.Database;

namespace Payments.ReadModel.Implementation {
    public class PaymentsReadDbContext: DbContext {
        public const string SchemaName = PaymentsDbContext.SchemaName;

        public PaymentsReadDbContext(string nameOrConnectionString): base(nameOrConnectionString) {}

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ThirdPartyProcessorPaymentDetails>().ToTable("ThirdPartyProcessorPaymentDetailsView",
                SchemaName);
        }

        public T Find<T>(Guid id) where T: class {
            return this.Set<T>().Find(id);
        }

        public IQueryable<T> Query<T>() where T: class {
            return this.Set<T>();
        }
    }
}