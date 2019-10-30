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
                {"properties", new Dictionary<string, object>
                {
                    {"ShapeName", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Shape Name"},
                        {"description", "Name for your data source in Sisense"},
                    }},
                }},
                {"required", new []
                {
                    "ShapeName"
                }}
            };

//            var schemaJsonObj = new Dictionary<string, object>();

            return JsonConvert.SerializeObject(schemaJsonObj);
        }
    }
}