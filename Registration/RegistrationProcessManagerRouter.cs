using System;
using System.Diagnostics;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Processes;
using Payments.Contracts.Events;
using Registration.Commands;
using Registration.Contracts.Events;
using Registration.Events;

namespace Registration {
    public class RegistrationProcessManagerRouter :
    IEventHandler<OrderPlaced>,
    IEventHandler<OrderUpdated>,
    IEnvelopedEventHandler<SeatsReserved>,
    IEventHandler<PaymentCompleted>,
    IEventHandler<OrderConfirmed>,
    ICommandHandler<ExpireRegistrationProcess> {
        private readonly Func<IProcessManagerDataContext<RegistrationProcessManager>> _contextFactory;
        public RegistrationProcessManagerRouter(Func<IProcessManagerDataContext<RegistrationProcessManager>> contextFactory) {
            this._contextFactory = contextFactory;
        }

        public void Handle(OrderPlaced @event) {
            using(var context = this._contextFactory.Invoke()) {
                var pm = context.Find(x => x.OrderId == @event.SourceId);
                if(pm==null) {
                    pm = new RegistrationProcessManager();
                }

                pm.Handle(@event);
                context.Save(pm);
            }
        }

        public void Handle(OrderUpdated @event) {
            using(var context = this._contextFactory.Invoke()) {
                var pm = context.Find(x => x.OrderId == @event.SourceId);
                if(pm != null) {
                    pm.Handle(@event);
                    context.Save(pm);
                }
                else {
                    Trace.TraceError(
                        "Failed to locate the registration process manager handling the order with id {0}.",
                        @event.SourceId);
                }
            }
        }

        public void Handle(Envelope<SeatsReserved> envelope) {
            using (var context = this._contextFactory.Invoke()) {
                var pm = context.Find(x => x.ReservationId == envelope.Body.ReservationId);
                if (pm != null) {
                    pm.Handle(envelope);
                    context.Save(pm);
                }
                else {
                    // TODO: should Cancel seat reservation!
                    Trace.TraceError(
                        "Failed to locate the registration process manager handling the seat reservation with id {0}. TODO: should Cancel seat reservation!",
                        envelope.Body.ReservationId);
                }
            }
        }

        public void Handle(PaymentCompleted @event) {
            using (var context = this._contextFactory.Invoke()) {
                var pm = context.Find(x => x.OrderId == @event.PaymentSourceId);
                if (pm != null) {
                    pm.Handle(@event);
                    context.Save(pm);
                }
                else {
                    Trace.TraceError(
                        "Failed to locate the registration process manager handling the paid order with id {0}.",
                        @event.PaymentSourceId);
                }
            }
        }

        public void Handle(OrderConfirmed @event) {
            using (var context = this._contextFactory.Invoke()) {
                var pm = context.Find(x => x.OrderId == @event.SourceId);
                if (pm != null) {
                    pm.Handle(@event);
                    context.Save(pm);
                }
                else {
                    Trace.TraceError(
                        "Failed to locate the registration process manager to complete with id {0}.",
                        @event.SourceId);
                }
            }
        }

        public void Handle(ExpireRegistrationProcess command) {
            using (var context = this._contextFactory.Invoke()) {
                var pm = context.Find(x => x.Id == command.ProcessId);
                if (pm != null) {
                    pm.Handle(command);
                    context.Save(pm);
                }
            }
        }
    }
}