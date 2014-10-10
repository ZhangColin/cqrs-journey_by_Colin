using System;

namespace Payments.ReadModel {
    /// <summary>
    /// 支付查询接口
    /// </summary>
    public interface IPaymentDao {
        ThirdPartyProcessorPaymentDetails GetThirdPartyProcessorPaymentDetails(Guid paymentId);
    }
}