
using System;
using Infrastructure.Messaging;

namespace Payments.Contracts.Commands {
    /// <summary>
    /// 取消第三方支付
    /// </summary>
    public class CancelThirdPartyProcessorPayment: ICommand {
        public Guid Id { get; private set; }
        public Guid PaymentId { get; set; }

        public CancelThirdPartyProcessorPayment() {
            this.Id = Guid.NewGuid();
        }
    }
}