using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PluginCouchDB.DataContracts;
using PluginCouchDB.Helper;
using Pub;

namespace PluginCouchDB.API.Replication
{
    public static partial class Replication
    {
        /// <summary>
        /// Adds and removes records to CouchDB
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="record"></param>
        /// <param name="config"></param>
        /// <returns>Error message string</returns>
        public static string WriteRecord(Schema schema, Record record, ConfigureReplicationFormData config)
        {
            try
            {
                var recordData = GetNamedRecordData(schema, record);

                return "";
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Converts data object with ids to friendly names
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="record"></param>
        /// <returns>Data object with friendly name keys</returns>
        private static Dictionary<string, object> GetNamedRecordData(Schema schema, Record record)
        {
            var namedData = new Dictionary<string, object>();
            var recordData = JsonConvert.DeserializeObject<Dictionary<string, object>>(record.DataJson);

            foreach (var property in schema.Properties)
            {
                var key = property.Id;
                if (recordData.ContainsKey(key))
                {
                    if (recordData[key] == null)
                    {
                        continue;
                    }
                    
                    namedData.Add(property.Name, recordData[key]);
                }
            }

            return namedData;
        }
    }
}