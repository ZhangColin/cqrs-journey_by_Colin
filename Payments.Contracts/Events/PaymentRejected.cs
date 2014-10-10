using System;
using Infrastructure.Messaging;

namespace Payments.Contracts.Events {
    /// <summary>
    /// 支付被拒绝
    /// </summary>
    public class PaymentRejected: IEvent {
        public Guid SourceId { get; set; }
        public Guid PaymentSourceId { get; set; } 
    }
}