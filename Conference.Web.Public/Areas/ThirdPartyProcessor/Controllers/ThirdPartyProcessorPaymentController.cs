using System.Web.Mvc;

namespace Conference.Web.Public.Areas.ThirdPartyProcessor.Controllers {
    public class ThirdPartyProcessorPaymentController: Controller {
        [HttpGet]
        public ActionResult Pay(string itemName, decimal itemAmount, string returnUrl, string cancelReturnUrl) {
            this.ViewBag.ItemName = itemName;
            this.ViewBag.ItemAmount = itemAmount;
            this.ViewBag.ReturnUrl = returnUrl;
            this.ViewBag.CancelReturnUrl = cancelReturnUrl;

            return this.View();
        }

        [HttpPost]
        public ActionResult Pay(string paymentResult, string returnUrl, string cancelReturnUrl) {
            string url;
            if(paymentResult == "accepted") {
                url = returnUrl;
            }
            else {
                url = cancelReturnUrl;
            }

            return this.Redirect(url);
        }
    }
}