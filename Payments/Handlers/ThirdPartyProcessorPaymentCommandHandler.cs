using System;
using System.Diagnostics;
using System.Linq;
using Infrastructure.Database;
using Infrastructure.Messaging.Handling;
using Payments.Contracts.Commands;

namespace Payments.Handlers {
    /// <summary>
    /// 支付相关命令处理器
    /// </summary>
    public class ThirdPartyProcessorPaymentCommandHandler:
        ICommandHandler<InitiateThirdPartyProcessorPayment>,
        ICommandHandler<CompleteThirdPartyProcessorPayment>,
        ICommandHandler<CancelThirdPartyProcessorPayment> {

        private Func<IDataContext<ThirdPartyProcessorPayment>> _contextFactory;
        public ThirdPartyProcessorPaymentCommandHandler(Func<IDataContext<ThirdPartyProcessorPayment>> contextFactory) {
            this._contextFactory = contextFactory;
        }

        public void Handle(InitiateThirdPartyProcessorPayment command) {
            using(var repository = this._contextFactory()) {
                var items = command.Items.Select(t => new ThirdPartyProcessorPaymentItem(t.Description, t.Amount));
                var payment = new ThirdPartyProcessorPayment(command.PaymentId, command.PaymentSourceId,
                    command.Description, command.TotalAmount, items);

                repository.Save(payment);
            }
        }

        public void Handle(CompleteThirdPartyProcessorPayment command) {
            using (var repository = this._contextFactory()) {
                var payment = repository.Find(command.PaymentId);

                if(payment!=null) {
                    payment.Complete();
                    repository.Save(payment);
                }
                else {
                    Trace.TraceError("Failed to locate the payment entity with id {0} for the completed third party payment", command.PaymentId);
                }
            }
        }

        public void Handle(CancelThirdPartyProcessorPayment command) {
            using (var repository = this._contextFactory()) {
                var payment = repository.Find(command.PaymentId);

                if (payment != null) {
                    payment.Cancel();
                    repository.Save(payment);
                }
                else {
                    Trace.TraceError("Failed to locate the payment entity with id {0} for the cancelled third party payment", command.PaymentId);
                }
            }
        }
    }
}