using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginCouchDB.API.Replication
{
    public static partial class Replication
    {
        public static string GetUIJson()
        {
            var uiJsonObj = new Dictionary<string, object>
            {
                {
                    "ui:order", new[]
                    {
                        "host",
                        "port",
                        "instance",
                        "DatabaseName",
                        "auth",
                        "username",
                        "password"
                    }
                },
                {
                    "password", new Dictionary<string, object>
                    {
                        {"ui:widget", "password"}
                    }
                }
            };

//            var uiJsonObj = new Dictionary<string, object>();

            return JsonConvert.SerializeObject(uiJsonObj);
        }
    }
}