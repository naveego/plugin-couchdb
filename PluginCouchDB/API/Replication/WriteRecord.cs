using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

                // get all the golden records from the database
                var response = await client.GetAsync($"/{databaseName}/_all_docs");
                response.EnsureSuccessStatusCode();
                var goldenRecord = JObject.Parse(await response.Content.ReadAsStringAsync())["rows"]
                    .ToObject<List<JObject>>();

                // get and check previous record
                var previousRecord = goldenRecord.Find(r => record.RecordId == r.GetValue("id").ToString());
                if (previousRecord == null && recordData.Count != 0)
                {
                    // set previous record to current record
                    Logger.Info($"databaseName:{databaseName} | recordId: {record.RecordId} - INSERT");
                    var currentGoldenRecord = new GoldenRecord
                    {
                        RecordId = record.RecordId,
                        Data = recordData
                    };
                    var createDocUri = $"/{databaseName}/{record.RecordId}";
                    await client.PutAsync(createDocUri,
                        new StringContent(JsonConvert.SerializeObject(currentGoldenRecord)));
                    return "";
                }

                if (recordData.Count == 0)
                {
                    // delete everything for this record
                    if (previousRecord != null)
                    {
                        Logger.Info($"databaseName:{databaseName} | recordId: {record.RecordId} - DELETE");
                        var deleteDocUri =
                            $"/{databaseName}/{record.RecordId}?{previousRecord.GetValue("value")["rev"]}";
                        await client.DeleteAsync(deleteDocUri);
                    }
                }
                else
                {
                    // update record and remove/add version
                    Logger.Info($"shapeId; {databaseName} | recordId: {record.RecordId} - UPSERT");
                    var reviseDocUri = $"/{databaseName}/{record.RecordId}";
                    // add _rev to recordData
                    recordData.Add("_rev", previousRecord.GetValue("value")["rev"]);
                    await client.PutAsync(reviseDocUri, new StringContent(JsonConvert.SerializeObject(recordData)));
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