using System;
using System.ComponentModel.DataAnnotations;

namespace Registration.ReadModel {
    public class SeatType {
        [Key]
        public Guid Id { get; set; }
        public Guid ConferenceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public int AvailableQuantity { get; set; }
        public int SeatsAvailabilityVersion { get; set; }

        protected SeatType() { }

        public SeatType(Guid id, Guid conferenceId, string name, string description, decimal price, int quantity) {
            this.Id = id;
            this.ConferenceId = conferenceId;
            this.Name = name;
            this.Description = description;
            this.Price = price;
            this.Quantity = quantity;

            this.AvailableQuantity = 0;
            this.SeatsAvailabilityVersion = -1;
        }
    }
}