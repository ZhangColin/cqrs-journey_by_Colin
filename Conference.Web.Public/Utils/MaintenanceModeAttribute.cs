using System.Web.Mvc;
using Conference.Common;

namespace Conference.Web.Public.Utils {
    public class MaintenanceModeAttribute: ActionFilterAttribute {
        public override void OnActionExecuting(ActionExecutingContext filterContext) {
            if(MaintenanceMode.IsInMaintainanceMode) {
                filterContext.Result = new ViewResult(){ViewName = "MaintenanceMode"};
            }
            else {
                base.OnActionExecuting(filterContext);
            }
        }
    }
}