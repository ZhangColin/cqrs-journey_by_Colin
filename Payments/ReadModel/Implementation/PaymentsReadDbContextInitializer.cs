using System.Data.Entity;
using System.Linq;
using Payments.Database;

namespace Payments.ReadModel.Implementation {
    public class PaymentsReadDbContextInitializer: IDatabaseInitializer<PaymentsDbContext> {
        private readonly IDatabaseInitializer<PaymentsDbContext> _innerInitializer;

        public PaymentsReadDbContextInitializer(IDatabaseInitializer<PaymentsDbContext> innerInitializer) {
            this._innerInitializer = innerInitializer;
        }

        public void InitializeDatabase(PaymentsDbContext context) {
            this._innerInitializer.InitializeDatabase(context);

            if (!context.Database.SqlQuery<int>("SELECT object_id FROM sys.views WHERE object_id = OBJECT_ID(N'[" + PaymentsReadDbContext.SchemaName + "].[ThirdPartyProcessorPaymentDetailsView]')").Any()) {
                CreateViews(context);
            }

            context.SaveChanges();
        }

        public static void CreateViews(PaymentsDbContext context) {
            context.Database.ExecuteSqlCommand(@"
CREATE VIEW " + PaymentsReadDbContext.SchemaName + @".[ThirdPartyProcessorPaymentDetailsView]
AS
SELECT     
    Id AS Id, 
    StateValue as StateValue,
    PaymentSourceId as PaymentSourceId,
    Description as Description,
    TotalAmount as TotalAmount
FROM " + PaymentsDbContext.SchemaName + ".ThirdPartyProcessorPayments");
        }
    }
}