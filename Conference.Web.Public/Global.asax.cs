using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Conference.Common;
using Conference.Web.Public.Utils;
using Infrastructure.BlobStorage;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Sql.BlobStorage;
using Infrastructure.Sql.Messaging;
using Infrastructure.Sql.Messaging.Implementation;
using Microsoft.Practices.Unity;
using Payments.ReadModel;
using Payments.ReadModel.Implementation;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;
using Unity.Mvc5;

namespace Conference.Web.Public
{
    public class MvcApplication : System.Web.HttpApplication {
        private IUnityContainer _container;

        protected void Application_Start()
        {
            MaintenanceMode.RefreshIsInMaintainanceMode();

            DatabaseSetup.Initialize();

            this._container = CreateContainer();

            DependencyResolver.SetResolver(new UnityDependencyResolver(this._container));

            GlobalFilters.Filters.Add(new MaintenanceModeAttribute());
            GlobalFilters.Filters.Add(new HandleErrorAttribute());

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", 
            "CA2000:Dispose objects before losing scope")]
        private IUnityContainer CreateContainer() {
            var container = new UnityContainer();
            try {
                container.RegisterType<ConferenceRegistrationDbContext>(new TransientLifetimeManager(),
                    new InjectionConstructor("ConferenceRegistration"));
                container.RegisterType<PaymentsReadDbContext>(new TransientLifetimeManager(),
                    new InjectionConstructor("Payments"));

                var cache = new MemoryCache("ReadModel");
                container.RegisterType<IOrderDao, OrderDao>();
                container.RegisterType<IConferenceDao, CachingConferenceDao>(
                    new ContainerControlledLifetimeManager(),
                    new InjectionConstructor(new ResolvedParameter<ConferenceDao>(), cache));
                container.RegisterType<IPaymentDao, PaymentDao>();

                var serializer = new JsonTextSerializer();
                container.RegisterInstance<ITextSerializer>(serializer);

                container.RegisterType<IBlobStorage, SqlBlobStorage>(new ContainerControlledLifetimeManager(),
                    new InjectionConstructor("BlobStorage"));
                container.RegisterType<IMessageSender, MessageSender>("Commands", new TransientLifetimeManager(),
                    new InjectionConstructor(Database.DefaultConnectionFactory, "SqlBus", "SqlBus.Commands"));
                container.RegisterType<ICommandBus, CommandBus>(new ContainerControlledLifetimeManager(),
                    new InjectionConstructor(new ResolvedParameter<IMessageSender>("Commands"), serializer));

                return container;
            }
            catch(Exception) {
                container.Dispose();
                throw;
            }
        }
    }
}
