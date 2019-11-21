using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginCouchDB.API.Replication
{
    public static partial class Replication
    {
        public static string GetSchemaJson()
        {
            var schemaJsonObj = new Dictionary<string, object>
            {
                {"type", "object"},
                {
                    "properties", new Dictionary<string, object>
                    {
                        {
                            "DatabaseName", new Dictionary<string, string>
                            {
                                {"type", "string"},
                                {"title", "Database Name"},
                                {"description", "Name for your data source in CouchDB"},
                            }
                        },
                    }
                },
                {
                    "required", new[]
                    {
                        "DatabaseName"
                    }
                }
            };

            return JsonConvert.SerializeObject(schemaJsonObj);
        }
    }
}