using System.Data.Entity;
using Conference.Common.Entity;
using Infrastructure.Sql.BlobStorage;
using Infrastructure.Sql.EventSourcing;
using Infrastructure.Sql.MessageLog;
using Payments.Database;
using Payments.ReadModel.Implementation;
using Registration.Database;
using Registration.ReadModel.Implementation;

namespace WorkerRoleCommandProcessor {
    public class DatabaseSetup {
        public static void Initialize() {
            Database.DefaultConnectionFactory =
                new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);

            Database.SetInitializer<ConferenceRegistrationDbContext>(null);
            Database.SetInitializer<RegistrationProcessManagerDbContext>(null);
            Database.SetInitializer<EventStoreDbContext>(null);
            Database.SetInitializer<MessageLogDbContext>(null);
            Database.SetInitializer<BlobStorageDbContext>(null);
            Database.SetInitializer<PaymentsDbContext>(null);
            Database.SetInitializer<PaymentsReadDbContext>(null);
        } 
    }
}