//                            _ooOoo_
//                           o8888888o
//                           88" . "88
//                           (| -_- |)
//                            O\ = /O
//                        ____/`---'\____
//                      .   ' \\| |// `.
//                       / \\||| : |||// \
//                     / _||||| -:- |||||- \
//                       | | \\\ - /// | |
//                     | \_| ''\---/'' | |
//                      \ .-\__ `-` ___/-. /
//                   ___`. .' /--.--\ `. . __
//                ."" '< `.___\_<|>_/___.' >'"".
//               | | : `- \`.;`\ _ /`;.`/ - ` : | |
//                 \ \ `-. \_ __\ /__ _/ .-` / /
//         ======`-.____`-.___\_____/___.-`____.-'======
//                            `=---='
//
//         .............................................
//                  佛祖保佑                  永无Bug
//                  本模块已经经过开光处理，绝无可能再产生Bug

using System.Data.Entity;
using System.Web.Mvc;
using System.Web.Routing;
using Conference.Common;
using Conference.Web.Admin.Utils;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Sql.Messaging;
using Infrastructure.Sql.Messaging.Implementation;

namespace Conference.Web.Admin
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static IEventBus EventBus { get; private set; }
        protected void Application_Start()
        {
            MaintenanceMode.RefreshIsInMaintainanceMode();

            DatabaseSetup.Initialize();

            AreaRegistration.RegisterAllAreas();

            GlobalFilters.Filters.Add(new MaintenanceModeAttribute());
            GlobalFilters.Filters.Add(new HandleErrorAttribute());

            RouteConfig.RegisterRoutes(RouteTable.Routes);

            var serializer = new JsonTextSerializer();

            EventBus = new EventBus(new MessageSender(Database.DefaultConnectionFactory, "SqlBus", "SqlBus.events"),
                serializer);

        }
    }
}
