using System;
using Infrastructure.Messaging;

namespace Payments.Contracts.Events {
    /// <summary>
    /// 支付初始化
    /// </summary>
    public class PaymentInitiated: IEvent {
        public Guid SourceId { get; set; }
        public Guid PaymentSourceId { get; set; } 
    }
}