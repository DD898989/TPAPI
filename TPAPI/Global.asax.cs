using System.Web;
using System.Web.Http;
using TPAPI.Provider;
using TPAPI.Models.Table;
using NLog;

namespace TPAPI
{
    public class WebApiApplication : HttpApplication
    {
        public static bool EnableLiveChat = false;
        Logger logger = LogManager.GetCurrentClassLogger();
        protected void Application_Start()
        {
            Migrate.Start();

            if (EnableLiveChat)
            {
                LiveChatController.Run();
            }

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}