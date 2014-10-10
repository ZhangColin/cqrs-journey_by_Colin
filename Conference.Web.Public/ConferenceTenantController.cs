using System.Web.Mvc;
using Registration.ReadModel;

namespace Conference.Web.Public {
    public abstract class ConferenceTenantController: AsyncController {
        private ConferenceAlias _conferenceAlias;
        private string _conferenceCode;

        protected ConferenceTenantController(IConferenceDao conferenceDao) {
            this.ConferenceDao = conferenceDao;
        }

        public IConferenceDao ConferenceDao { get; private set; }

        public string ConferenceCode {
            get {
                return this._conferenceCode
                    ?? (this._conferenceCode = (string)ControllerContext.RouteData.Values["conferenceCode"]);
            }
            internal set { this._conferenceCode = value; }
        }

        public ConferenceAlias ConferenceAlias {
            get {
                return this._conferenceAlias
                    ?? (this._conferenceAlias = this.ConferenceDao.GetConferenceAlias(this.ConferenceCode));
            }
            internal set { this._conferenceAlias = value; }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext) {
            base.OnActionExecuting(filterContext);

            if(!string.IsNullOrEmpty(this.ConferenceCode) && this.ConferenceAlias == null) {
                filterContext.Result = new HttpNotFoundResult("Invalid conference code.");
            }
        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext) {
            base.OnResultExecuting(filterContext);

            if(filterContext.Result is ViewResultBase) {
                this.ViewBag.Conference = this.ConferenceAlias;
            }
        }
    }
}