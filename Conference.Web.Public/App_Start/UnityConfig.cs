using System.Data.Entity;
using System.Runtime.Caching;
using System.Web.Mvc;
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
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
			var container = new UnityContainer();

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

            
            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }
    }
}