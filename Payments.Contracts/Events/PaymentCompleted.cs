using System;
using Infrastructure.Messaging;

namespace Payments.Contracts.Events {
    /// <summary>
    /// 支付完成
    /// </summary>
    public class PaymentCompleted: IEvent {
        public Guid SourceId { get; set; }
        public Guid PaymentSourceId { get; set; } 
    }
}