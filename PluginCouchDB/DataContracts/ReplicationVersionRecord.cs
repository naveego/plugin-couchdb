using System.Collections.Generic;

namespace PluginCouchDB.DataContracts
{
    public class ReplicationVersionRecord
    {
        public string VersionRecordId { get; set; }
        
        public string GoldenRecordId { get; set; }
        
        public Dictionary<string, object> Data { get; set; }
    }
}