using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payments.ReadModel {
    /// <summary>
    /// 第三方支付处理明细
    /// </summary>
    public class ThirdPartyProcessorPaymentDetails {
        [Key]
        public Guid Id { get; private set; }
        public int StateValue { get; private set; }

        [NotMapped]
        public ThirdPartyProcessorPayment.States State {
            get { return (ThirdPartyProcessorPayment.States)this.StateValue; }
            set { this.StateValue = (int)value; }
        }
        public Guid PaymentSourceId { get; private set; }
        public string Description { get; private set; }
        public decimal TotalAmount { get; private set; }
        protected ThirdPartyProcessorPaymentDetails() {}

        protected ThirdPartyProcessorPaymentDetails(Guid id, ThirdPartyProcessorPayment.States state, Guid paymentSourceId,
            string description, decimal totalAmount) {
            this.Id = id;
            this.State = state;
            this.PaymentSourceId = paymentSourceId;
            this.Description = description;
            this.TotalAmount = totalAmount;
        }
    }
}