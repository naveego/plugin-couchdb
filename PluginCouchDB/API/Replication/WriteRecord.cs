using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PluginCouchDB.DataContracts;
using PluginCouchDB.Helper;
using Pub;
using Newtonsoft.Json.Linq;

namespace PluginCouchDB.API.Replication
{
    public static partial class Replication
    {
        /// <summary>
        /// Adds and removes records to local database
        /// Adds and updates available shapes
        /// </summary>
        /// <param name="client"></param>
        /// <param name="schema"></param>
        /// <param name="record"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task<string> WriteRecord(RequestHelper client, Schema schema, Record record,
            ConfigureReplicationFormData config)
        {
            try
            {
                var recordData = GetNamedRecordData(schema, record);
                var databaseName = string.Concat(config.DatabaseName.Where(c => !char.IsWhiteSpace(c)));
                var primaryKey = config.PrimaryKey;

                //generate record id for CouchDB 
                var recordId = primaryKey.Equals("auto generate unique id")
                    ? record.RecordId
                    : recordData[primaryKey].ToString();
                Logger.Info($"receive record: {recordId}");

                //remove "_id" and "_rev" fields from record data
                if (recordData.ContainsKey("_id")) recordData.Remove("_id");
                if (recordData.ContainsKey("_rev")) recordData.Remove("_rev");

                // find previous record in database, return null if it doesn't exist
                var findQuery = $"{{\"selector\":{{\"_id\": {{\"$eq\":\"{recordId}\"}}}}}}";
                var previousRecordResponse = await client.PostAsync($"{databaseName}/_find", new StringContent(
                    findQuery, Encoding.UTF8,
                    "application/json"));
                previousRecordResponse.EnsureSuccessStatusCode();
                var documents = JObject.Parse(await previousRecordResponse.Content.ReadAsStringAsync())["docs"];

                // get and check previous record
                Logger.Info("get previous record");
                var previousRecord = documents.ToList().FirstOrDefault();
                Logger.Info($"previous record: {previousRecord}");

                if (previousRecord == null && recordData.Count != 0)
                {
                    // set previous record to current record
                    Logger.Info($"databaseName:{databaseName} | recordId: {record.RecordId} - INSERT");
                    var createDocUri = $"/{databaseName}/{recordId}";
                    await client.PutAsync(createDocUri,
                        new StringContent(JsonConvert.SerializeObject(recordData), Encoding.UTF8, "application/json"));
                    return "";
                }

                if (previousRecord != null)
                {
                    if (recordData.Count == 0)
                    {
                        Logger.Info($"databaseName:{databaseName} | recordId: {record.RecordId} - DELETE");
                        var deleteDocUri =
                            $"/{databaseName}/{recordId}?{previousRecord["rev"]}";
                        await client.DeleteAsync(deleteDocUri);
                    }
                    else
                    {
                        // update record and remove/add version
                        Logger.Info($"shapeId; {databaseName} | recordId: {record.RecordId} - UPSERT");
                        var reviseDocUri = $"/{databaseName}/{recordId}";
                        // add _rev to recordData
                        recordData.Add("_rev", previousRecord["rev"]);
                        await client.PutAsync(reviseDocUri,
                            new StringContent(JsonConvert.SerializeObject(recordData), Encoding.UTF8,
                                "application/json"));
                    }
                }

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