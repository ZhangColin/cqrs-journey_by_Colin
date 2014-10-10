using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conference;
using Infrastructure.Sql.BlobStorage;
using Infrastructure.Sql.EventSourcing;
using Infrastructure.Sql.MessageLog;
using Infrastructure.Sql.Messaging.Implementation;
using Payments.Database;
using Payments.ReadModel.Implementation;
using Registration.Database;
using Registration.ReadModel.Implementation;

namespace DatabaseInitializer {
    class Program {
        static void Main(string[] args) {
            var connectionString = ConfigurationManager.AppSettings["defaultConnection"];

            if(args.Length>0) {
                connectionString = args[0];
            }

            using(var context = new ConferenceContext(connectionString)) {
                if(context.Database.Exists()) {
                    context.Database.Delete();
                }

                context.Database.Create();
            }

            Database.SetInitializer<EventStoreDbContext>(null);
            Database.SetInitializer<MessageLogDbContext>(null);
            Database.SetInitializer<BlobStorageDbContext>(null);
            Database.SetInitializer<ConferenceRegistrationDbContext>(null);
            Database.SetInitializer<RegistrationProcessManagerDbContext>(null);
            Database.SetInitializer<PaymentsDbContext>(null);

            (new DbContext[] {
                new EventStoreDbContext(connectionString),
                new MessageLogDbContext(connectionString),
                new BlobStorageDbContext(connectionString), 
                new PaymentsDbContext(connectionString), 
                new RegistrationProcessManagerDbContext(connectionString), 
                new ConferenceRegistrationDbContext(connectionString), 
            }).ToList().ForEach(context => {
                var adapter = (IObjectContextAdapter)context;
                var script = adapter.ObjectContext.CreateDatabaseScript();
                context.Database.ExecuteSqlCommand(script);
                context.Dispose();
            });

            using(var context = new ConferenceRegistrationDbContext(connectionString)) {
                ConferenceRegistrationDbContextInitializer.CreateIndexs(context);
            }

            using(var context = new RegistrationProcessManagerDbContext(connectionString)) {
                RegistrationProcessManagerDbContextInitializer.CreateIndexes(context);
            }

            using(var context = new PaymentsDbContext(connectionString)) {
                PaymentsReadDbContextInitializer.CreateViews(context);
            }

            MessagingDbInitializer.CreateDatabaseObjects(connectionString, "SqlBus");
        }
    }
}
