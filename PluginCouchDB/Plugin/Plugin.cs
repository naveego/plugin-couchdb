using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluginCouchDB.API.Discover;
using PluginCouchDB.API.Read;
using PluginCouchDB.API.Replication;
using PluginCouchDB.DataContracts;
using PluginCouchDB.Helper;
using Pub;

namespace PluginCouchDB.Plugin
{
    public class Plugin : Publisher.PublisherBase
    {
        private RequestHelper _client;
        private readonly HttpClient _injectedClient;
        private readonly ServerStatus _server;
        private TaskCompletionSource<bool> _tcs;

        public Plugin(HttpClient client = null)
        {
            _injectedClient = client != null ? client : new HttpClient();
            _server = new ServerStatus
            {
                Connected = false,
                WriteConfigured = false
            };
        }

        /// <summary>
        /// Establishes a connection with CouchDB API. Creates an authenticated http client and tests it.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>A message indicating connection success</returns>
        public override async Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
        {
            // validate settings passed in
            try
            {
                _server.Settings = JsonConvert.DeserializeObject<Settings>(request.SettingsJson);
                _server.Settings.Validate();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return new ConnectResponse
                {
                    OauthStateJson = request.OauthStateJson,
                    ConnectionError = "",
                    OauthError = "",
                    SettingsError = e.Message
                };
            }

            // create new authenticated request helper with validated settings
            try
            {
                _client = new RequestHelper(_server.Settings, _injectedClient);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }

            // attempt to call the CouchDB API api
            try
            {
                var response = await _client.GetAsync($"{_server.Settings.DatabaseName}/_all_dbs");
                response.EnsureSuccessStatusCode();

                _server.Connected = true;
                Logger.Info("Connected to CouchDB API");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);

                return new ConnectResponse
                {
                    OauthStateJson = request.OauthStateJson,
                    ConnectionError = e.Message,
                    OauthError = "",
                    SettingsError = ""
                };
            }

            return new ConnectResponse
            {
                ConnectionError = "",
                OauthError = "",
                SettingsError = ""
            };
        }

        public override async Task ConnectSession(ConnectRequest request,
            IServerStreamWriter<ConnectResponse> responseStream, ServerCallContext context)
        {
            Logger.Info("Connecting session...");

            // create task to wait for disconnect to be called
            _tcs?.SetResult(true);
            _tcs = new TaskCompletionSource<bool>();

            // call connect method
            var response = await Connect(request, context);

            await responseStream.WriteAsync(response);

            Logger.Info("Session connected.");

            // wait for disconnect to be called
            await _tcs.Task;
        }


        /// <summary>
        /// Discovers schemas located in the users CouchDB instance
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>Discovered schemas</returns>
        public override async Task<DiscoverSchemasResponse> DiscoverSchemas(DiscoverSchemasRequest request,
            ServerCallContext context)
        {
            Logger.Info("Discovering Schemas...");

            DiscoverSchemasResponse discoverSchemasResponse = new DiscoverSchemasResponse();

            // only return requested schemas if refresh mode selected
            if (request.Mode == DiscoverSchemasRequest.Types.Mode.All)
            {
                Logger.Info("Plugin does not support auto schema discovery.");
                return discoverSchemasResponse;
            }

            try
            {
                var refreshSchemas = request.ToRefresh;

                Logger.Info($"Refresh schemas attempted: {refreshSchemas.Count}");

                var tasks = refreshSchemas.Select(GetSchemaProperties)
                    .ToArray();

                await Task.WhenAll(tasks);

                discoverSchemasResponse.Schemas.AddRange(tasks.Where(x => x.Result != null).Select(x => x.Result));

                // return all schemas
                Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");

                return discoverSchemasResponse;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Publishes a stream of data for a given schema
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task ReadStream(ReadRequest request, IServerStreamWriter<Record> responseStream,
            ServerCallContext context)
        {
            var schema = request.Schema;
            var limit = request.Limit;
            var limitFlag = request.Limit != 0;

            Logger.Info($"Publishing records for schema: {schema.Name}");

            try
            {
                // check if query is empty
                if (string.IsNullOrWhiteSpace(schema.Query))
                {
                    Logger.Info("Query not defined.");
                    return;
                }

                // build read record couchDB query
                ReadQuery readQuery = new ReadQuery
                {
                    fields = new List<string>(),
                    selector = JsonConvert.SerializeObject("{}")
                };

                // set limit to couchDB query if limitFlag is on
                if (limitFlag)
                {
                    readQuery.limit = limit;
                }

                foreach (Property property in schema.Properties)
                {
                    readQuery.fields.Add(property.Name);
                }

                var couchdbReadRecordQuery = JsonConvert.SerializeObject(readQuery);

                // read record from couchDB
                Logger.Info($"Reading records from {_server.Settings.DatabaseName} database");
                var readRecordUri = $"{_server.Settings.DatabaseName}/_find";
                var response = await _client.PostAsync(readRecordUri, new StringContent(couchdbReadRecordQuery));
                response.EnsureSuccessStatusCode();

                var documents = JObject.Parse(await response.Content.ReadAsStringAsync())["docs"];

                // build record map
                if (documents.ToList().Count > 0)
                {
                    foreach (JObject document in documents)
                    {
                        // build record map
                        var recordMap = new Dictionary<string, object>();
                        foreach (JProperty property in document.Properties())
                        {
                            recordMap[property.Name] = property.Value;
                        }

                        //create record
                        var record = new Record
                        {
                            Action = Record.Types.Action.Upsert,
                            DataJson = JsonConvert.SerializeObject(recordMap)
                        };

                        //publish record
                        await responseStream.WriteAsync(record);
                    }
                }
                else
                {
                    Logger.Info("No record read from database.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Configures replication writebacks to CouchDB
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ConfigureReplicationResponse> ConfigureReplication(ConfigureReplicationRequest request,
            ServerCallContext context)
        {
            Logger.Info("Configuring write...");

            var schemaJson = Replication.GetSchemaJson();
            var uiJson = Replication.GetUIJson();

            try
            {
                return Task.FromResult(new ConfigureReplicationResponse
                {
                    Form = new ConfigurationFormResponse
                    {
                        DataJson = request.Form.DataJson,
                        Errors = { },
                        SchemaJson = schemaJson,
                        UiJson = uiJson,
                        StateJson = request.Form.StateJson
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return Task.FromResult(new ConfigureReplicationResponse
                {
                    Form = new ConfigurationFormResponse
                    {
                        DataJson = request.Form.DataJson,
                        Errors = {e.Message},
                        SchemaJson = schemaJson,
                        UiJson = uiJson,
                        StateJson = request.Form.StateJson
                    }
                });
            }
        }

        /// <summary>
        /// Prepares writeback settings to write to CouchDB
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<PrepareWriteResponse> PrepareWrite(PrepareWriteRequest request, ServerCallContext context)
        {
            Logger.Info("Preparing write...");
            _server.WriteConfigured = false;

            var writeSettings = new WriteSettings
            {
                CommitSLA = request.CommitSlaSeconds,
                Schema = request.Schema,
                Replication = request.Replication
            };

            _server.WriteSettings = writeSettings;
            _server.WriteConfigured = true;

            Logger.Info("Write prepared.");
            return Task.FromResult(new PrepareWriteResponse());
        }

        /// <summary>
        /// Writes records to CouchDB
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task WriteStream(IAsyncStreamReader<Record> requestStream,
            IServerStreamWriter<RecordAck> responseStream, ServerCallContext context)
        {
            try
            {
                Logger.Info("Writing records to CouchDB...");

                var schema = _server.WriteSettings.Schema;
                var sla = _server.WriteSettings.CommitSLA;
                var inCount = 0;
                var outCount = 0;

                // get next record to publish while connected and configured
                while (await requestStream.MoveNext(context.CancellationToken) && _server.Connected &&
                       _server.WriteConfigured)
                {
                    var record = requestStream.Current;
                    inCount++;

                    Logger.Debug($"Got record: {record.DataJson}");

                    if (_server.WriteSettings.IsReplication())
                    {
                        var config =
                            JsonConvert.DeserializeObject<ConfigureReplicationFormData>(_server.WriteSettings
                                .Replication.SettingsJson);

                        // send record to source system
                        // timeout if it takes longer than the sla
                        var task = Task.Run(() => Replication.WriteRecord(_client, schema, record, config));
                        if (task.Wait(TimeSpan.FromSeconds(sla)))
                        {
                            // send ack
                            var ack = new RecordAck
                            {
                                CorrelationId = record.CorrelationId,
                                Error = task.Result
                            };
                            await responseStream.WriteAsync(ack);

                            if (String.IsNullOrEmpty(task.Result))
                            {
                                outCount++;
                            }
                        }
                        else
                        {
                            // send timeout ack
                            var ack = new RecordAck
                            {
                                CorrelationId = record.CorrelationId,
                                Error = "timed out"
                            };
                            await responseStream.WriteAsync(ack);
                        }
                    }
                    else
                    {
                        throw new Exception("Only replication writeback are supported");
                    }
                }

                Logger.Info($"Wrote {outCount} of {inCount} records to CouchDB.");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Handles disconnect requests from the agent
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<DisconnectResponse> Disconnect(DisconnectRequest request, ServerCallContext context)
        {
            // clear connection
            _server.Connected = false;
            _server.Settings = null;

            // alert connection session to close
            if (_tcs != null)
            {
                _tcs.SetResult(true);
                _tcs = null;
            }

            Logger.Info("Disconnected");
            return Task.FromResult(new DisconnectResponse());
        }

        /// <summary>
        /// Gets a schema for a given query
        /// </summary>
        /// <param name="schema"></param>
        /// <returns>A schema or null</returns>
        private async Task<Schema> GetSchemaProperties(Schema schema)
        {
            try
            {
                //check if query is empty or invalid json
                if (string.IsNullOrWhiteSpace(schema.Query) || !IsValidJson(schema.Query))
                {
                    Logger.Error("Invalid schema query");
                    return null;
                }

                // add "_id", "_rev" as required field
                var schemaQueryJson = JObject.Parse(schema.Query);
                var getSchemaUri = $"{_server.Settings.DatabaseName}/_find";
                var response = await _client.PostAsync(getSchemaUri,
                    new StringContent(Discover.GetValidSchemaQuery(schemaQueryJson)));
                response.EnsureSuccessStatusCode();

                var documents = JObject.Parse(await response.Content.ReadAsStringAsync())["docs"];

                // get each field and create a property for the field
                Logger.Info($"Getting property type for all {documents.ToList().Count} documents");
                if (documents.ToList().Count > 0)
                {
                    var discoveredPropertyTypes = Discover.GetPropertyTypes(documents, 100);
                    foreach (KeyValuePair<string, Dictionary<PropertyType, int>> entry in discoveredPropertyTypes)
                    {
                        var propertyTypeofMaxValue = entry.Value.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                        // create property
                        var property = new Property
                        {
                            Id = entry.Key,
                            Name = entry.Key,
                            Description = "",
                            Type = propertyTypeofMaxValue,
                            IsKey = false,
                            IsCreateCounter = false,
                            IsUpdateCounter = false,
                            PublisherMetaJson = ""
                        };
                        schema.Properties.Add(property);
                    }
                }
                else
                {
                    schema = null;
                }

                return schema;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return null;
            }
        }

        private static bool IsValidJson(string strInput)
        {
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Logger.Error(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Logger.Error(ex.ToString());
                    return false;
                }
            }
            else
            {
                Logger.Error("Content must be application/json");
                return false;
            }
        }
    }
}