using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conference {
    public class Order {
        public enum OrderStatus {
            Pending,
            Paid
        }

        public Order(Guid conferenceId, Guid orderId, string accessCode): this() {
            this.Id = orderId;
            this.ConferenceId = conferenceId;
            this.AccessCode = accessCode;
        }

        protected Order() {
            this.Seats = new ObservableCollection<OrderSeat>();
        }

        [Key]
        public Guid Id { get; set; }
        public Guid ConferenceId { get; set; }
        public Guid? AssignmentsId { get; set; }

        [Display(Name = "Order Code")]
        public string AccessCode { get; set; }
        [Display(Name = "Registrant Name")]
        public string RegistrantName { get; set; }
        [Display(Name = "Registrant Email")]
        public string RegistrantEmail { get; set; }
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [NotMapped]
        public OrderStatus Status {
            get { return (OrderStatus)this.StatusValue; }
            set { this.StatusValue = (int)value; }
        }

        public int StatusValue { get; set; }

        public ICollection<OrderSeat> Seats { get; set; } 
    }
}