using System.Data.Entity;

namespace Payments.Database {
    /// <summary>
    /// 支付数据库上下文
    /// </summary>
    public class PaymentsDbContext: DbContext {
        public const string SchemaName = "ConferencePayments";

        public PaymentsDbContext(string nameOrConnectionString): base(nameOrConnectionString) {}

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ThirdPartyProcessorPayment>().ToTable("ThirdPartyProcessorPayments", SchemaName);
            modelBuilder.Entity<ThirdPartyProcessorPaymentItem>().ToTable("ThirdPartyProcessorPaymentItems", SchemaName);
        }

        public DbSet<ThirdPartyProcessorPayment> ThirdPartyProcessorPayments { get; set; } 
    }
}