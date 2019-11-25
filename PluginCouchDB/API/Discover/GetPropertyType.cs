using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Pub;

namespace PluginCouchDB.API.Discover
{
    public static partial class Discover
    {
        /// <summary>
        /// Get property types for all the properties in a document
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="limit"></param>
        /// <returns>Dictionary</returns>
        public static Dictionary<string, Dictionary<PropertyType, int>> GetPropertyTypes(JToken documents, int limit)
        {
            var docCount = documents.ToList().Count;
            var readDocsCount = 0;
            var discoveredPropertyType = new Dictionary<string, Dictionary<PropertyType, int>>();
            foreach (JObject document in documents)
            {
                if (readDocsCount >= Math.Min(limit, docCount)) break;
                foreach (JProperty property in document.Properties())
                {
                    if (!discoveredPropertyType.ContainsKey(property.Name))
                    {
                        discoveredPropertyType[property.Name] = new Dictionary<PropertyType, int>
                        {
                            {GetPropertyType(property.Value.ToString()), 1}
                        };
                    }
                    else
                    {
                        if (discoveredPropertyType[property.Name].ContainsKey(GetPropertyType(property.Value.ToString())))
                        {
                            discoveredPropertyType[property.Name][GetPropertyType(property.Value.ToString())] += 1;
                        }
                        else
                        {
                            discoveredPropertyType[property.Name].Add(GetPropertyType(property.Value.ToString()), 1);
                        }
                        
                    }
                }
                readDocsCount++;
            }

            return discoveredPropertyType;
        }

        /// <summary>
        /// get datatype for one property
        /// </summary>
        /// <param name="propertyVal"></param>
        /// <returns>propertyType</returns>
        private static PropertyType GetPropertyType(string propertyVal)
        {
            switch (true)
            {
                case bool _ when Boolean.TryParse(propertyVal, out Boolean b):
                    return PropertyType.Bool;
                case bool _ when int.TryParse(propertyVal, out int i):
                case bool _ when long.TryParse(propertyVal, out long l):
                    return PropertyType.Integer;
                case bool _ when float.TryParse(propertyVal, out float f):
                case bool _ when double.TryParse(propertyVal, out double d):
                    return PropertyType.Float;
                case bool _ when DateTime.TryParse(propertyVal, out DateTime D):
                    return PropertyType.Datetime;
                default:
                    return PropertyType.String;
            }
        }
    }
}
