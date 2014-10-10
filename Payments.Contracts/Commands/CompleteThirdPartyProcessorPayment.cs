
using System;
using Infrastructure.Messaging;

namespace Payments.Contracts.Commands {
    /// <summary>
    /// 完成第三方支付
    /// </summary>
    public class CompleteThirdPartyProcessorPayment: ICommand {
        public Guid Id { get; private set; }
        public Guid PaymentId { get; set; }

        public CompleteThirdPartyProcessorPayment() {
            this.Id = Guid.NewGuid();
        }
    }
}