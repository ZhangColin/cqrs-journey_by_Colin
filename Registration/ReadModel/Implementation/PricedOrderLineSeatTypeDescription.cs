using System;
using System.ComponentModel.DataAnnotations;

namespace Registration.ReadModel.Implementation {
    internal class PricedOrderLineSeatTypeDescription {
        [Key]
        public Guid SeatTypeId { get; set; }
        public string Name { get; set; }
    }
}