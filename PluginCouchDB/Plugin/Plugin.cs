using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Newtonsoft.Json;
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

            // attempt to call the Legacy API api
            try
            {
                var response = await _client.GetAsync("connection");
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
        /// Discovers schemas located in the users Zoho CRM instance
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>Discovered schemas</returns>
        public override async Task<DiscoverSchemasResponse> DiscoverSchemas(DiscoverSchemasRequest request,
            ServerCallContext context)
        {
            Logger.Info("Discovering Schemas...");

            // get a schema for each module found
            DiscoverSchemasResponse discoverSchemasResponse = new DiscoverSchemasResponse();
//            try
//            {
//                var schemas = await Discover.GetAllReadSchemas(_client, _server.Settings);
//                
//                discoverSchemasResponse.Schemas.AddRange(schemas);
//            }
//            catch (Exception e)
//            {
//                Logger.Error(e.Message);
//                throw;
//            }
//
//            Logger.Info($"Schemas found: {discoverSchemasResponse.Schemas.Count}");
//
//            // only return requested schemas if refresh mode selected
//            if (request.Mode == DiscoverSchemasRequest.Types.Mode.Refresh)
//            {
//                var refreshSchemaIds = request.ToRefresh.Select(x => x.Id);
//                var schemas =
//                    JsonConvert.DeserializeObject<Schema[]>(
//                        JsonConvert.SerializeObject(discoverSchemasResponse.Schemas));
//                discoverSchemasResponse.Schemas.Clear();
//                discoverSchemasResponse.Schemas.AddRange(schemas.Where(x => refreshSchemaIds.Contains(x.Id)));
//                
//
//                Logger.Debug($"Schemas found: {JsonConvert.SerializeObject(schemas)}");
//                Logger.Debug($"Refresh requested on schemas: {refreshSchemaIds}");
//
//                Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");
//                return discoverSchemasResponse;
//            }
//
//            // return all schemas otherwise
//            Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");
            return discoverSchemasResponse;
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
            //Read.GetAllRecords()
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
                        Errors = { e.Message },
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
                        var config = JsonConvert.DeserializeObject<ConfigureReplicationFormData>(_server.WriteSettings.Replication.SettingsJson);
                        
                        // send record to source system
                        // timeout if it takes longer than the sla
                        var task = Task.Run(() => Replication.WriteRecord(schema, record, config));
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
                        // TODO: write backs
                        throw new Exception("Only replication writebacks are supported");
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
    }
}