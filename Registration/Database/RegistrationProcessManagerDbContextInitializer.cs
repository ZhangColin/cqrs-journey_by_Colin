using System.Data.Entity;

namespace Registration.Database {
    public class RegistrationProcessManagerDbContextInitializer: IDatabaseInitializer<RegistrationProcessManagerDbContext> {
        private readonly IDatabaseInitializer<RegistrationProcessManagerDbContext> _innerInitializer;

        public RegistrationProcessManagerDbContextInitializer(
            IDatabaseInitializer<RegistrationProcessManagerDbContext> innerInitializer) {
            this._innerInitializer = innerInitializer;
        }

        public void InitializeDatabase(RegistrationProcessManagerDbContext context) {
            this._innerInitializer.InitializeDatabase(context);

            CreateIndexes(context);

            context.SaveChanges();
        }

        public static void CreateIndexes(RegistrationProcessManagerDbContext context) {
            context.Database.ExecuteSqlCommand(@"
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_RegistrationProcessmanager_Completed')
CREATE NONCLUSTERED INDEX IX_RegistrationProcessManager_Completed ON [" + RegistrationProcessManagerDbContext.SchemaName
                + @"].[RegistrationProcess]( Completed )

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_RegistrationProcessmanager_OrderId')
CREATE NONCLUSTERED INDEX IX_RegistrationProcessManager_OrderId ON [" + RegistrationProcessManagerDbContext.SchemaName
                + @"].[RegistrationProcess]( OrderId )
");
        }
    }
}