using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginCouchDB.API.Replication
{
    public static partial class Replication
    {
        public static string GetSchemaJson(List<string> schemaProperty)
        {
            var schemaJsonObj = new Dictionary<string, object>
            {
                {"type", "object"},
                {
                    "properties", new Dictionary<string, object>
                    {
                        {
                            "DatabaseName", new Dictionary<string, string>
                            {
                                {"type", "string"},
                                {"title", "Database Name"},
                                {"description", "Name for your data source in CouchDB"},
                            }
                        },
                        {
                            "PrimaryKey", new Dictionary<string, object>
                            {
                                {"type", "string"},
                                {
                                    "description",
                                    "Select property as primary key in CouchDB or we can auto generate unique id for you"
                                },
                                {"properties", new { }},
                                {
                                    "enum", schemaProperty.ToArray()
                                }
                            }
                        }
                    }
                },
                {
                    "required", new[]
                    {
                        "DatabaseName",
                        "PrimaryKey"
                    }
                }
            };

            return JsonConvert.SerializeObject(schemaJsonObj);
        }
    }
}