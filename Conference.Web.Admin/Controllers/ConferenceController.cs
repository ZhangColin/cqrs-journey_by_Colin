using System;
using System.Data;
using System.Data.Entity.Core;
using System.Web.Mvc;
using System.Web.Routing;
using AutoMapper;
using Infrastructure.Utils;

namespace Conference.Web.Admin.Controllers {
    public class ConferenceController: Controller {
        static ConferenceController() {
            Mapper.CreateMap<EditableConferenceInfo, ConferenceInfo>();
        }

        private ConferenceService _service;

        public ConferenceService Service {
            get { return _service ?? (_service = new ConferenceService(MvcApplication.EventBus)); }
        }

        public ConferenceInfo Conference { get; private set; }

        protected override void OnActionExecuting(ActionExecutingContext filterContext) {
            var slug = (string)this.ControllerContext.RequestContext.RouteData.Values["slug"];

            if(!string.IsNullOrEmpty(slug)) {
                this.ViewBag.Slug = slug;
                this.Conference = this.Service.FindConference(slug);

                if(this.Conference!=null) {
                    var accessCode = (string)this.ControllerContext.RequestContext.RouteData.Values["accessCode"];

                    if(accessCode == null || !string.Equals(accessCode, this.Conference.AccessCode, StringComparison.Ordinal)) {
                        filterContext.Result = new HttpUnauthorizedResult("Invalid access code.");
                    }
                    else {
                        this.ViewBag.OwnerName = this.Conference.OwnerName;
                        this.ViewBag.WasEverPublished = this.Conference.WasEverPublished;
                    }
                }
            }
            base.OnActionExecuting(filterContext);
        }

        #region 会议

        public ActionResult Locate() {
            return this.View();
        }

        [HttpPost]
        public ActionResult Locate(string email, string accessCode) {
            ConferenceInfo conference = this.Service.FindConference(email, accessCode);
            if(conference==null) {
                ModelState.AddModelError(string.Empty, "邮箱或密码不正确。");
                ViewBag.Email = email;

                return this.View();
            }

            return RedirectToAction("Index", new {slug = conference.Slug, accessCode});
        }

        public ActionResult Index() {
            if(this.Conference==null) {
                return this.HttpNotFound();
            }
            return this.View(this.Conference);
        }

        public ActionResult Create() {
            return this.View();
        }

        [HttpPost]
        public ActionResult Create(
            [Bind(Exclude = "Id, AccessCode, Seats, WasEverPublished")] ConferenceInfo conference) {
            if(ModelState.IsValid) {
                try {
                    conference.Id = GuidUtil.NewSequentialId();
                    this.Service.CreateConference(conference);
                }
                catch(DuplicateNameException e) {
                    ModelState.AddModelError("Slug", e.Message);
                }

                return RedirectToAction("Index", new {slug = conference.Slug, accessCode = conference.AccessCode});
            }

            return this.View(conference);
        }

        public ActionResult Edit() {
            if(this.Conference==null) {
                return this.HttpNotFound();
            }

            return this.View(this.Conference);
        }

        [HttpPost]
        public ActionResult Edit(EditableConferenceInfo conference) {
            if(this.Conference == null) {
                return this.HttpNotFound();
            }

            if(ModelState.IsValid) {
                var edited = Mapper.Map(conference, this.Conference);
                this.Service.UpdateConference(edited);

                return RedirectToAction("Index", new {slug = edited.Slug, accessCode = edited.AccessCode});
            }

            return this.View(this.Conference);
        }

        [HttpPost]
        public ActionResult Publish() {
            if (this.Conference == null) {
                return this.HttpNotFound();
            }

            this.Service.Publish(this.Conference.Id);

            return RedirectToAction("Index", new { slug = this.Conference.Slug, accessCode = this.Conference.AccessCode });
        }
        
        [HttpPost]
        public ActionResult Unpublish() {
            if (this.Conference == null) {
                return this.HttpNotFound();
            }

            this.Service.Unpublish(this.Conference.Id);

            return RedirectToAction("Index", new { slug = this.Conference.Slug, accessCode = this.Conference.AccessCode });
        }

        #endregion

        #region 座位类型

        public ViewResult Seats() {
            return this.View();
        }

        public ActionResult SeatGrid() {
            if (this.Conference == null) {
                return this.HttpNotFound();
            }

            return this.PartialView(this.Service.FindSeatTypes(this.Conference.Id));
        }

        public ActionResult SeatRow(Guid id) {
            return this.PartialView("SeatGrid", new SeatType[] {this.Service.FindSeatType(id)});
        }

        public ActionResult CreateSeat() {
            return this.PartialView("EditSeat");
        }

        [HttpPost]
        public ActionResult CreateSeat(SeatType seat) {
            if(this.Conference==null) {
                return this.HttpNotFound();
            }

            if(ModelState.IsValid) {
                seat.Id = GuidUtil.NewSequentialId();

                this.Service.CreateSeat(this.Conference.Id, seat);
                return this.PartialView("SeatGrid", new SeatType[] {seat});
            }

            return this.PartialView("EditSeat", seat);
        }

        public ActionResult EditSeat(Guid id) {
            if (this.Conference == null) {
                return this.HttpNotFound();
            }

            return this.PartialView(this.Service.FindSeatType(id));
        }

        [HttpPost]
        public ActionResult EditSeat(SeatType seat) {
            if (this.Conference == null) {
                return this.HttpNotFound();
            }

            if(ModelState.IsValid) {
                try {
                    this.Service.UpdateSeat(this.Conference.Id, seat);
                }
                catch(ObjectNotFoundException) {
                    return this.HttpNotFound();
                }

                return this.PartialView("SeatGrid", new SeatType[] {seat});
            }

            return this.PartialView(seat);
        }

        [HttpPost]
        public void DeleteSeat(Guid id) {
            this.Service.DeleteSeat(id);
        }

        #endregion

        #region Orders

        public ViewResult Orders() {
            var orders = this.Service.FindOrders(this.Conference.Id);
            return this.View(orders);
        }

        #endregion
    }
}