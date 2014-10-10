using System;
using System.Linq;

namespace Payments.ReadModel.Implementation {
    public class PaymentDao: IPaymentDao {
        private readonly Func<PaymentsReadDbContext> _contextFactory;

        public PaymentDao(Func<PaymentsReadDbContext> contextFactory) {
            this._contextFactory = contextFactory;
        }

        public ThirdPartyProcessorPaymentDetails GetThirdPartyProcessorPaymentDetails(Guid paymentId) {
            using(var repository = this._contextFactory()) {
                return repository.Query<ThirdPartyProcessorPaymentDetails>()
                    .FirstOrDefault(dto => dto.Id == paymentId);
            }
        }
    }
}