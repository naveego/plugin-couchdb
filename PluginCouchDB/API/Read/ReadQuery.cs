using System.Collections.Generic;

namespace PluginCouchDB.API.Read
{
    public class ReadQuery
    {
        public List<string> fields { get; set; }

        public uint limit { get; set; }

        public string selector { get; set; }
    }
}