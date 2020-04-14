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
                        "DatabaseName",
                        "PrimaryKey"
                    }
                }
            };
            
            return JsonConvert.SerializeObject(uiJsonObj);
        }
    }
}