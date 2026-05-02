using System.Data.Entity;
using System.Web;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Migrations;
using AlcoConnectWatch.Services;

namespace AlcoConnectWatch
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AlcoConnectWatchContext, Configuration>());

            using (var context = new AlcoConnectWatchContext())
            {
                context.Database.Initialize(false);
            }

            FileWatcherService.Instance.Start();
        }

        protected void Application_End()
        {
            FileWatcherService.Instance.Stop();
        }
    }
}
