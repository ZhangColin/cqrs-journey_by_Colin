using System;
using System.Threading;
using System.Web.Mvc;
using Infrastructure.Messaging;
using Payments.Contracts.Commands;
using Payments.ReadModel;

namespace Conference.Web.Public.Controllers {
    public class PaymentController: Controller {
        private const int _waitTimeoutInSeconds = 5;
        private readonly ICommandBus _commandBus;
        private readonly IPaymentDao _paymentDao;

        public PaymentController(ICommandBus commandBus, IPaymentDao paymentDao) {
            this._commandBus = commandBus;
            this._paymentDao = paymentDao;
        }

        public ActionResult ThirdPartyProcessorPayment(string conferenceCode, Guid paymentId, string paymentAcceptedUrl,
            string paymentRejectedUrl) {
            var returnUrl = Url.Action("ThirdPartyProcessorPaymentAccepted",
                new {conferenceCode, paymentId, paymentAcceptedUrl});
            var cancelReturnUrl = Url.Action("ThirdPartyProcessorPaymentRejected",
                new {conferenceCode, paymentId, paymentRejectedUrl});

            var paymentDto = this.WaitUntilAvailable(paymentId);

            if(paymentDto == null) {
                return this.View("WaitForPayment");
            }

            var paymentProcessorUrl = this.Url.Action("Pay", "ThirdPartyProcessorPayment", new {
                area = "ThirdPartyProcessor",
                item = paymentDto.Description,
                itemAmount = paymentDto.TotalAmount,
                returnUrl,
                cancelReturnUrl
            });

            return this.Redirect(paymentProcessorUrl);
        }

        private ThirdPartyProcessorPaymentDetails WaitUntilAvailable(Guid paymentId) {
            var deadline = DateTime.Now.AddSeconds(_waitTimeoutInSeconds);

            while(DateTime.Now<deadline) {
                var paymentDto = this._paymentDao.GetThirdPartyProcessorPaymentDetails(paymentId);

                if(paymentDto!=null) {
                    return paymentDto;
                }

                Thread.Sleep(500);
            }

            return null;
        }

        public ActionResult ThirdPartyProcessorPaymentAccepted(string conferenceCode, Guid paymentId,
            string paymentAcceptedUrl) {
            this._commandBus.Send(new CompleteThirdPartyProcessorPayment {PaymentId = paymentId});
            return this.Redirect(paymentAcceptedUrl);
        }

        public ActionResult ThirdPartyProcessorPaymentRejected(string conferenceCode, Guid paymentId,
            string paymentRejectedUrl) {
            this._commandBus.Send(new CancelThirdPartyProcessorPayment {PaymentId = paymentId});
            return this.Redirect(paymentRejectedUrl);
        }


    }
}