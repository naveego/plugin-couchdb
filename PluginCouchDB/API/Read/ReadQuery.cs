using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PluginCouchDB.API.Read
{
    public class ReadQuery
    {
        public List<string> fields { get; set; }

        [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
        public uint? limit { get; set; }

        public JObject selector { get; set; }
    }
}