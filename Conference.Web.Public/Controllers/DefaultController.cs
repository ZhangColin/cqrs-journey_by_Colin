using System.Web.Mvc;
using Registration.ReadModel;

namespace Conference.Web.Public.Controllers {
    public class DefaultController: Controller {
        private readonly IConferenceDao _dao;

        public DefaultController(IConferenceDao dao) {
            this._dao = dao;
        }

        public ActionResult Index() {
            return this.View(this._dao.GetPublishedConferences());
        }
    }
}