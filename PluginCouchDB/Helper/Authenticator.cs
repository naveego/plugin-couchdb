using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PluginCouchDB.Helper
{
    public class Authenticator
    {
        private readonly HttpClient _client;
        private readonly Settings _settings;
        private string _token;

        public Authenticator(Settings settings, HttpClient client)
        {
            _client = client;
            _settings = settings;
            _token = string.Empty;
        }

        /// <summary>
        /// Get a token for the CouchDB API
        /// </summary>
        /// <returns></returns>
        public string GetToken()
        {
            // check if token is expired or will expire in 5 minutes or less
            if (_token == string.Empty)
            {
                _token = $"{_settings.Username}:{_settings.Password}";
            }
            
            // return saved token
            return _token;
        }
    }
}