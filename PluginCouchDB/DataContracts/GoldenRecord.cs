using System.Collections.Generic;

namespace PluginCouchDB.DataContracts
{
    public class GoldenRecord
    {
        public string RecordId { get; set; }
        
        public Dictionary<string, object> Data { get; set; }
    }
}