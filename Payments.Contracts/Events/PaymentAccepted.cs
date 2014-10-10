using System;
using Infrastructure.Messaging;

namespace Payments.Contracts.Events {
    /// <summary>
    /// 支付确认
    /// </summary>
    public class PaymentAccepted: IEvent {
        public Guid SourceId { get; set; }
        public Guid PaymentSourceId { get; set; }
    }
}