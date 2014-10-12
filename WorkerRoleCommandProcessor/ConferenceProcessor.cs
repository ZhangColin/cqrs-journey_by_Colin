using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using Conference;
using Infrastructure;
using Infrastructure.BlobStorage;
using Infrastructure.Database;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Processes;
using Infrastructure.Serialization;
using Infrastructure.Sql.BlobStorage;
using Infrastructure.Sql.Database;
using Infrastructure.Sql.EventSourcing;
using Infrastructure.Sql.MessageLog;
using Infrastructure.Sql.Messaging;
using Infrastructure.Sql.Messaging.Handling;
using Infrastructure.Sql.Messaging.Implementation;
using Infrastructure.Sql.Processes;
using Microsoft.Practices.Unity;
using Payments;
using Payments.Database;
using Payments.Handlers;
using Registration;
using Registration.Database;
using Registration.Handlers;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace WorkerRoleCommandProcessor {
    public class ConferenceProcessor: IDisposable {
        private IUnityContainer _container;
        private CancellationTokenSource _cancellationTokenSource;
        private List<IProcessor> _processors;

        public ConferenceProcessor() {

            this._cancellationTokenSource = new CancellationTokenSource();
            this._container = CreateContainer();

            this._processors = this._container.ResolveAll<IProcessor>().ToList();
        }

        private IUnityContainer CreateContainer() {
            var container = new UnityContainer();

            container.RegisterInstance<ITextSerializer>(new JsonTextSerializer());
            container.RegisterInstance<IMetadataProvider>(new StandardMetadataProvider());

            container.RegisterType<DbContext, RegistrationProcessManagerDbContext>("registration",
                new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistrationProcesses"));
            container.RegisterType<IProcessManagerDataContext<RegistrationProcessManager>,
                SqlProcessManagerDataContext<RegistrationProcessManager>>(
                    new TransientLifetimeManager(),
                    new InjectionConstructor(new ResolvedParameter<Func<DbContext>>("registration"),
                        typeof(ICommandBus), typeof(ITextSerializer)));

            container.RegisterType<DbContext, PaymentsDbContext>("payments", new TransientLifetimeManager(),
                new InjectionConstructor("Payments"));
            container.RegisterType<IDataContext<ThirdPartyProcessorPayment>,
                SqlDataContext<ThirdPartyProcessorPayment>>(
                    new TransientLifetimeManager(),
                    new InjectionConstructor(new ResolvedParameter<Func<DbContext>>("payments"), typeof(IEventBus)));

            container.RegisterType<ConferenceRegistrationDbContext>(new TransientLifetimeManager(),
                new InjectionConstructor("ConferenceRegistration"));

            container.RegisterType<IConferenceDao, ConferenceDao>(new ContainerControlledLifetimeManager());
            container.RegisterType<IOrderDao, OrderDao>(new ContainerControlledLifetimeManager());

            container.RegisterType<IPricingService, PricingService>(new ContainerControlledLifetimeManager());

            container.RegisterType<ICommandHandler, RegistrationProcessManagerRouter>("RegistrationProcessManagerRouter");
            container.RegisterType<ICommandHandler, OrderCommandHandler>("OrderCommandHandler");
            container.RegisterType<ICommandHandler, SeatsAvailabilityHandler>("SeatsAvailabilityHandler");
            container.RegisterType<ICommandHandler, ThirdPartyProcessorPaymentCommandHandler>("ThirdPartyProcessorPaymentCommandHandler");
            container.RegisterType<ICommandHandler, SeatAssignmentsHandler>("SeatAssignmentsHandler");

            container.RegisterType<ConferenceContext>(new TransientLifetimeManager(),
                new InjectionConstructor("ConferenceManagement"));

            var serializer = container.Resolve<ITextSerializer>();
            var metadata = container.Resolve<IMetadataProvider>();

            container.RegisterType<IBlobStorage, SqlBlobStorage>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor("BlobStorage"));

            var commandBus = new CommandBus(new MessageSender(Database.DefaultConnectionFactory,
                "SqlBus", "SqlBus.Commands"), serializer);
            var eventBus = new EventBus(
                new MessageSender(Database.DefaultConnectionFactory, "SqlBus", "SqlBus.Events"), serializer);

            var commandProcessor = new CommandProcessor(new MessageReceiver(Database.DefaultConnectionFactory,
                "SqlBus", "SqlBus.Commands"), serializer);
            var eventProcessor = new EventProcessor(new MessageReceiver(Database.DefaultConnectionFactory,
                "SqlBus", "SqlBus.Events"), serializer);

            container.RegisterInstance<ICommandBus>(commandBus);
            container.RegisterInstance<IEventBus>(eventBus);
            container.RegisterInstance<ICommandHandlerRegistry>(commandProcessor);
            container.RegisterInstance<IProcessor>("CommandProcessor", commandProcessor);
            container.RegisterInstance<IEventHandlerRegistry>(eventProcessor);
            container.RegisterInstance<IProcessor>("EventProcessor", eventProcessor);

            container.RegisterType<SqlMessageLog>(new InjectionConstructor("MessageLog", serializer, metadata));
            container.RegisterType<IEventHandler, SqlMessageLogHandler>("SqlMessageLogHandler");
            container.RegisterType<ICommandHandler, SqlMessageLogHandler>("SqlMessageLogHandler");

            RegisterRepository(container);
            RegisterEventHandlers(container, eventProcessor);
            RegisterCommandHandlers(container);

            return container;
        }

        private void RegisterCommandHandlers(UnityContainer container) {
            var commandHandlerRegistry = container.Resolve<ICommandHandlerRegistry>();

            foreach(var commandHandler in container.ResolveAll<ICommandHandler>()) {
                commandHandlerRegistry.Register(commandHandler);
            }
        }

        private void RegisterEventHandlers(UnityContainer container, EventProcessor eventProcessor) {
            eventProcessor.Register(container.Resolve<RegistrationProcessManagerRouter>());
            eventProcessor.Register(container.Resolve<DraftOrderViewModelGenerator>());
            eventProcessor.Register(container.Resolve<PricedOrderViewModelGenerator>());
            eventProcessor.Register(container.Resolve<ConferenceViewModelGenerator>());
            eventProcessor.Register(container.Resolve<SeatAssignmentsViewModelGenerator>());
            eventProcessor.Register(container.Resolve<SeatAssignmentsHandler>());
            eventProcessor.Register(container.Resolve<OrderEventHandler>());
            eventProcessor.Register(container.Resolve<SqlMessageLogHandler>());
        }

        private void RegisterRepository(UnityContainer container) {
            container.RegisterType<EventStoreDbContext>(new TransientLifetimeManager(),
                new InjectionConstructor("EventStore"));
            container.RegisterType(typeof(IEventSourcedRepository<>), typeof(SqlEventSourcedRepository<>),
                new ContainerControlledLifetimeManager());
        }

        public void Start() {
            this._processors.ForEach(p=>p.Start());
        }

        public void Stop() {
            this._cancellationTokenSource.Cancel();
            this._processors.ForEach(p=>p.Stop());
        }

        public void Dispose() {
            this._container.Dispose();
            this._cancellationTokenSource.Dispose();
        }
    }
}