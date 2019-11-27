using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Pub;
using RichardSzalay.MockHttp;
using Xunit;
using Record = Pub.Record;

namespace PluginCouchDB.Test
{
    public class PluginTest
    {
        private ConnectRequest GetConnectSettings()
        {
            return new ConnectRequest
            {
                SettingsJson =
                    "{\"Hostname\":\"hostname:5984\",\"Username\":\"test\",\"Password\":\"password\",\"DatabaseName\":\"DatabaseName\"}"
            };
        }

        private MockHttpMessageHandler GetMockHttpMessageHandler()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("http://test:password@hostname:5984/DatabaseName/_find")
                .Respond("application/json",
                    "{\"docs\":[{\"_id\":\"1000\", \"title\":\"Test1\"},{\"_id\":\"1001\",\"title\":\"Test2\"},{\"_id\":\"6e1295ed6c\", \"title\":\"Test3\"},{\"_id\":\"6e1295ed6b\",\"title\":\"Test4\"},{\"_id\":\"6e1295ed6e\",\"title\":\"Test5\"}]}");

            mockHttp.When("http://test:password@hostname:5984/_all_dbs")
                .Respond("application/json", "{}");

            mockHttp.When("http://test:password@hostname:5984/DatabaseName/_all_docs")
                .Respond("application/json",
                    "{\"rows\":[{\"id\":\"176694\", \"value\":{\"rev\":\"1-967\"}},{\"id\":\"176695\", \"value\":{\"rev\":\"1-968\"}}]}");

            mockHttp.When("http://hostname:5984/DatabaseName/new-id")
                .Respond("application/json", "{\"rows\":[{\"id\":\"176696\", \"value\":{\"rev\":\"1-969\"}}]}");


            return mockHttp;
        }

        private Schema GetTestSchema(string query)
        {
            return new Schema
            {
                Id = "test",
                Name = "test",
                Query = "{\"_id\":\"query\",\"title\":\"query\"}"
            };
        }

        private List<Record> GetTestRecords()
        {
            return new List<Record>
            {
                new Record
                {
                    RecordId = "new-id",
                    CorrelationId = "test",
                    Action = Record.Types.Action.Insert,
                    DataJson = "{\"_id\":\"12345\",\"title\":\"test\",\"_rev\":\"334432\"}"
                }
            };
        }

        [Fact]
        public async Task ConnectTest()
        {
            // set up

            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin.Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
                Ports = {new ServerPort("127.0.0.1", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"127.0.0.1:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings();

            // act
            var response = client.Connect(request);

            // assert
            Assert.IsType<ConnectResponse>(response);

            // clean up
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasAllTest()
        {
            // set up
            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin.Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Empty(response.Schemas);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasRefreshTest()
        {
            // set up
            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin.Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {GetTestSchema("DiscoverSchemas")}
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal("test", schema.Id);
            Assert.Equal("test", schema.Name);
            Assert.Equal(2, schema.Properties.Count);

            var firstProperty = schema.Properties[0];
            Assert.Equal("_id", firstProperty.Id);
            Assert.Equal("_id", firstProperty.Name);
            Assert.Equal(PropertyType.String, firstProperty.Type);

            var secondProperty = schema.Properties[1];
            Assert.Equal("title", secondProperty.Id);
            Assert.Equal("title", secondProperty.Name);
            Assert.Equal(PropertyType.String, secondProperty.Type);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamTest()
        {
            // setup

            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin.Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new ReadRequest()
            {
                Schema = new Schema
                {
                    Id = "[Customers.address]",
                    Properties =
                    {
                        new Property {Id = "_id", Name = "_id", Type = PropertyType.Integer},
                        new Property {Id = "title", Name = "title", Type = PropertyType.String}
                    },
                    Query = "query"
                },
                Limit = 2
            };

            // act
            client.Connect(connectRequest);
            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(5, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task PrepareWriteTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin.Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new PrepareWriteRequest()
            {
                Schema = new Schema
                {
                    Id = "Test",
                    Properties =
                    {
                        new Property
                        {
                            Id = "Id",
                            Type = PropertyType.String,
                            IsKey = true
                        },
                        new Property
                        {
                            Id = "Name",
                            Type = PropertyType.String
                        }
                    }
                },
                CommitSlaSeconds = 5
            };

            // act
            client.Connect(connectRequest);
            var response = client.PrepareWrite(request);

            // assert
            Assert.IsType<PrepareWriteResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task WriteStreamReplicationTest()
        {
            // set up
            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin.Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();
            var prepareRequest = new PrepareWriteRequest()
            {
                Schema = new Schema
                {
                    Id = "Test",
                    Properties =
                    {
                        new Property
                        {
                            Id = "_id",
                            Name = "_id",
                            Type = PropertyType.String,
                        },
                        new Property
                        {
                            Id = "title",
                            Name = "title",
                            Type = PropertyType.String
                        }
                    }
                },
                CommitSlaSeconds = 10,
                Replication = new ReplicationWriteRequest
                {
                    SettingsJson = "{\"DatabaseName\":\"DatabaseName\"}",
                }
            };

            var records = GetTestRecords();

            var recordAcks = new List<RecordAck>();

            // act
            client.Connect(connectRequest);
            client.PrepareWrite(prepareRequest);

            using (var call = client.WriteStream())
            {
                var responseReaderTask = Task.Run(async () =>
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var ack = call.ResponseStream.Current;
                        recordAcks.Add(ack);
                    }
                });

                foreach (Record record in records)
                {
                    await call.RequestStream.WriteAsync(record);
                }

                await call.RequestStream.CompleteAsync();
                await responseReaderTask;
            }

            // assert
            Assert.Single(recordAcks);
            Assert.Equal("test", recordAcks[0].CorrelationId);

            // clean up
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DisconnectTest()
        {
            // set up
            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin.Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
                Ports = {new ServerPort("127.0.0.1", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"127.0.0.1:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = new DisconnectRequest();

            // act
            var response = client.Disconnect(request);

            // assert
            Assert.IsType<DisconnectResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}