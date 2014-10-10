using System;
using System.ComponentModel.DataAnnotations;

namespace Payments.ReadModel {
    /// <summary>
    /// 支付项
    /// </summary>
    public class PaymentItem {
        [Key]
        public Guid Id { get; private set; }
        public string Description { get; private set; }
        public decimal Price { get; private set; }

        protected PaymentItem() {}

        public PaymentItem(Guid id, string description, decimal price) {
            this.Id = id;
            this.Description = description;
            this.Price = price;
        }
    }
}