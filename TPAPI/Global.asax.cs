using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TPAPI.Models;
using TPAPI.Models.Table;
using TPAPI.Provider;
using System.Configuration;
using RestSharp;
using System.Text;
using Newtonsoft.Json.Linq;
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