using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Registration.ReadModel {
    public class OrderSeats {
        [Key]
        public Guid AssignmentsId { get; set; }
        public Guid OrderId { get; set; }

        public IList<OrderSeat> Seats { get; set; } 

        public OrderSeats(Guid assignmentsId, Guid orderId, IEnumerable<OrderSeat> seats) {
            this.AssignmentsId = assignmentsId;
            this.OrderId = orderId;
            this.Seats = seats.ToList();
        }

        public OrderSeats() {
            this.Seats = new List<OrderSeat>();
        }
    }
}