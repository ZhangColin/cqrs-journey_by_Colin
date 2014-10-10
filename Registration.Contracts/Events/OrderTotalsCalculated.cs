using Infrastructure.EventSourcing;

namespace Registration.Contracts.Events {
    /// <summary>
    /// 
    /// </summary>
    public class OrderTotalsCalculated: VersionedEvent {
        public decimal Total { get; set; }
        public OrderLine[] Lines { get; set; }
        public bool IsFreeOfCharge { get; set; }
    }
}