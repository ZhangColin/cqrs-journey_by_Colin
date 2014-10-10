using System.Web.Mvc;
using Registration.ReadModel;

namespace Conference.Web.Public.Controllers {
    public class ConferenceController: Controller {
        private readonly IConferenceDao _dao;
        public ConferenceController(IConferenceDao dao) {
            this._dao = dao;
        }

        public ActionResult Display(string conferenceCode) {
            var conference = this._dao.GetConferenceDetails(conferenceCode);
            return this.View(conference);
        }
    }
}