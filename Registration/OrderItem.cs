using System;

namespace Registration {
    public class OrderItem {
        public Guid SeatType { get; private set; }
        public int Quantity { get; private set; }

        public OrderItem(Guid seatType, int quantity) {
            this.SeatType = seatType;
            this.Quantity = quantity;
        }
    }
}