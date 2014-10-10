using System;
using Infrastructure.Utils;

namespace Payments {
    public class ThirdPartyProcessorPaymentItem {
        public Guid Id { get; private set; }
        public string Description { get; private set; }
        public decimal Amount { get; private set; }
        protected ThirdPartyProcessorPaymentItem() {}

        public ThirdPartyProcessorPaymentItem(string description, decimal amount) {
            this.Id = GuidUtil.NewSequentialId();

            this.Description = description;
            this.Amount = amount;
        }
    }
}
