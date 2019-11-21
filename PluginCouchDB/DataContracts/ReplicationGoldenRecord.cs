using System.Collections.Generic;

namespace PluginCouchDB.DataContracts
{
    public class ReplicationGoldenRecord
    {
        public string RecordId { get; set; }
        
        public List<string> VersionRecordIds { get; set; }
        
        public Dictionary<string, object> Data { get; set; }
    }
}