using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using Conference.Common;
using Infrastructure.Database;
using Infrastructure.Messaging;
using Payments.Contracts.Events;

namespace Payments {
    public class ThirdPartyProcessorPayment: IAggregateRoot, IEventPublisher {
        public enum States {
            Initiated = 0,
            Accepted = 1, 
            Completed = 2,
            Rejected = 3
        }

        private List<IEvent> _events = new List<IEvent>();

        protected ThirdPartyProcessorPayment() {
            this.Items = new ObservableCollection<ThirdPartyProcessorPaymentItem>();
        }

        public ThirdPartyProcessorPayment(Guid id, Guid paymentSourceId, string description, decimal totalAmount, 
            IEnumerable<ThirdPartyProcessorPaymentItem> items):this() {
            this.Id = id;
            this.PaymentSourceId = paymentSourceId;
            this.Description = description;
            this.TotalAmount = totalAmount;
            this.Items.AddRange(items);

            this.AddEvent(new PaymentInitiated {SourceId = id, PaymentSourceId = paymentSourceId});
        }

        private void AddEvent(IEvent @event) {
            this._events.Add(@event);
        }

        public ICollection<ThirdPartyProcessorPaymentItem> Items { get; private set; }

        public Guid Id { get; private set; }
        public Guid PaymentSourceId { get; private set; }
        public string Description { get; private set; }
        public decimal TotalAmount { get; private set; }
        public IEnumerable<IEvent> Events { get { return _events; } }
        public int StateValue { get; private set; }

        [NotMapped]
        public States State {
            get { return (States)this.StateValue; }
            internal set { this.StateValue = (int)value; }
        }

        public void Complete() {
            if(this.State!=States.Initiated) {
                throw new InvalidOperationException();
            }

            this.State = States.Completed;
            this.AddEvent(new PaymentCompleted(){SourceId = this.Id, PaymentSourceId = this.PaymentSourceId});
        }
        
        public void Cancel() {
            if(this.State!=States.Initiated) {
                throw new InvalidOperationException();
            }

            this.State = States.Rejected;
            this.AddEvent(new PaymentRejected(){SourceId = this.Id, PaymentSourceId = this.PaymentSourceId});
        }
    }
}