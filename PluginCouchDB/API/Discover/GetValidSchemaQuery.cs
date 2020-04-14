using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PluginCouchDB.API.Discover
{
    public static partial class Discover
    {
        /// <summary>
        /// convert schema query to valid couchDB query
        /// </summary>
        /// <param name="schemaQuery"></param>
        /// <returns>string</returns>
        public static string GetValidSchemaQuery(JObject schemaQuery)
        {
            if (schemaQuery["fields"] != null)
            {
                var queryFields = schemaQuery["fields"].ToObject<List<string>>();
                if (!queryFields.Contains("_id"))
                {
                    queryFields.Add("_id");
                }

                if (!queryFields.Contains("_rev"))
                {
                    queryFields.Add("_rev");
                }

                schemaQuery["fields"] = JArray.FromObject(queryFields);
            }
            else
            {
                schemaQuery.Add("fields", JArray.FromObject(new List<string> {"_id", "_rev"}));
            }

            return JsonConvert.SerializeObject(schemaQuery);
        }
    }
}
