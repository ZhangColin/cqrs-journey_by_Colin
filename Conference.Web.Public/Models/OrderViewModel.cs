using System;
using System.Collections.Generic;

namespace Conference.Web.Public.Models {
    public class OrderViewModel {
        public OrderViewModel() {
            this.Items = new List<OrderItemViewModel>();
        }

        public Guid OrderId { get; set; }
        public int OrderVersion { get; set; }
        public Guid ConferenceId { get; set; }
        public string ConferenceCode { get; set; }
        public string ConferenceName { get; set; }
        public IList<OrderItemViewModel> Items { get; set; }
        public long ReservationExpirationDate { get; set; }
    }
}