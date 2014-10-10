using System;

namespace Registration.ReadModel {
    public class DraftOrderItem {
        public Guid SeatType { get; set; }
        public Guid OrderId { get; set; }
        public int RequestedSeats { get; set; }
        public int ReservedSeats{ get; set; }

        public DraftOrderItem(Guid seatType, int requestedSeats) {
            this.SeatType = seatType;
            this.RequestedSeats = requestedSeats;
        }

        protected DraftOrderItem() { }
    }
}