using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conference {
    public class OrderSeat {
        protected OrderSeat() {
            this.Attendee=new Attendee();
        }

        public OrderSeat(Guid orderId, int position, Guid seatInfoId): this() {
            this.OrderId = orderId;
            this.Position = position;
            this.SeatInfoId = seatInfoId;
        }

        public int Position { get; set; }
        public Guid OrderId { get; set; }
        public Attendee Attendee { get; set; }

        [ForeignKey("SeatInfo")]
        public Guid SeatInfoId { get; set; }
        public SeatType SeatInfo { get; set; }
    }
}