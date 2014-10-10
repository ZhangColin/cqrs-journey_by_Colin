using System.Data.Entity;
using Conference.Common.Entity;
using Infrastructure.Sql.BlobStorage;
using Payments.ReadModel.Implementation;
using Registration.ReadModel.Implementation;

namespace Conference.Web.Public {
    public class DatabaseSetup {
        public static void Initialize() {
            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);

            Database.SetInitializer<BlobStorageDbContext>(null);
            Database.SetInitializer<PaymentsReadDbContext>(null);
            Database.SetInitializer<ConferenceRegistrationDbContext>(null);
        }
    }
}