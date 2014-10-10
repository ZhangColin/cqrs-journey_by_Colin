using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Registration.ReadModel {
    public class PricedOrder {
        [Key]
        public Guid OrderId { get; set; }

        public Guid? AssignmentsId { get; set; }

        public List<PricedOrderLine> Lines { get; set; }
        public decimal Total { get; set; }
        public int OrderVersion { get; set; }
        public bool IsFreeOfCharge { get; set; }
        public DateTime? ReservationExpirationDate { get; set; }
    }
}