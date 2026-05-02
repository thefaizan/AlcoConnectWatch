using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AlcoConnectWatch
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // CORS Configuration - Restrict to specific origins in production
            // For development, allow localhost. In production, replace with your actual domain.
            var cors = new EnableCorsAttribute(
                origins: "http://localhost:5050,http://localhost:56060,https://yourdomain.com",
                headers: "*",
                methods: "GET,POST,PUT,DELETE,OPTIONS"
            );
            config.EnableCors(cors);

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
            jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonSettings.DateFormatString = "yyyy-MM-dd";
            jsonSettings.NullValueHandling = NullValueHandling.Include;
            jsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
