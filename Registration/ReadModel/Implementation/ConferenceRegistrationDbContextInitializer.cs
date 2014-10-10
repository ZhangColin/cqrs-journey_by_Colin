using System.Data.Entity;

namespace Registration.ReadModel.Implementation {
    public class ConferenceRegistrationDbContextInitializer: IDatabaseInitializer<ConferenceRegistrationDbContext> {
        private readonly IDatabaseInitializer<ConferenceRegistrationDbContext> _innerInitializer;

        public ConferenceRegistrationDbContextInitializer(IDatabaseInitializer<ConferenceRegistrationDbContext> innerInitializer) {
            this._innerInitializer = innerInitializer;
        }

        public void InitializeDatabase(ConferenceRegistrationDbContext context) {
            this._innerInitializer.InitializeDatabase(context);

            CreateIndexs(context);

            context.SaveChanges();
        }

        public static void CreateIndexs(DbContext context) {
            context.Database.ExecuteSqlCommand(@"
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_SeatTypesView_ConferenceId')
CREATE NONCLUSTERED INDEX IX_SeatTypesView_ConferenceId ON [" + ConferenceRegistrationDbContext.SchemaName
                + "].[ConferenceSeatTypesView]( ConferenceId )");
        }
    }
}