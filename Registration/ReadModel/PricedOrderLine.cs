using System;

namespace Registration.ReadModel {
    public class PricedOrderLine {
        public Guid OrderId { get; set; }
        public int Position { get; set; }
        public string Description { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}