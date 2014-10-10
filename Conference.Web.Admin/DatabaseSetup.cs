using System.Data.Entity;
using Conference.Common.Entity;

namespace Conference.Web.Admin {
    public class DatabaseSetup {
        public static void Initialize() {
            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);

            Database.SetInitializer<ConferenceContext>(null);
        }
    }
}