using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Registration.ReadModel {
    public class DraftOrder {
        [Key]
        public Guid OrderId { get; private set; }
        public Guid ConferenceId { get; private set; }
        public int OrderVersion { get; internal set; }

        public DateTime? ReservationExpirationDate { get; set; }
        public string RegistrantEmail { get; internal set; }
        public string AccessCode { get; internal set; }
        public ICollection<DraftOrderItem> Lines { get; private set; }
        public int StateValue { get; private set; }

        [NotMapped]
        public States State {
            get { return (States)this.StateValue; }
            set { this.StateValue = (int)value; }
        }

        public enum States {
            PendingReservation,
            PartiallyReserved,
            ReservationCompleted,
            Rejected,
            Confirmed
        }

        protected DraftOrder() {
            this.Lines = new ObservableCollection<DraftOrderItem>();
        }

        public DraftOrder(Guid orderId, Guid conferenceId, States state, int orderVersion = 0): this() {
            this.OrderId = orderId;
            this.ConferenceId = conferenceId;
            this.State = state;
            this.OrderVersion = orderVersion;
        }
    }
}