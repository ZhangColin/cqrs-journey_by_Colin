using System;

namespace Registration.Contracts {
    /// <summary>
    /// 座位数量
    /// </summary>
    public class SeatQuantity {
        public Guid SeatType { get; set; }
        public int Quantity { get; set; }

        public SeatQuantity(Guid seatType, int quantity) {
            this.SeatType = seatType;
            this.Quantity = quantity;
        }
    }
}