using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PluginCouchDB.Helper
{
     public class RequestHelper
    {
        private readonly Authenticator _authenticator;
        private readonly HttpClient _client;
        private readonly Settings _settings;

        public RequestHelper(Settings settings, HttpClient client)
        {
            _authenticator = new Authenticator(settings, client);
            _client = client;
            _settings = settings;
        }

        /// <summary>
        /// Get Async request wrapper for making authenticated requests
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetAsync(string resource)
        {
            string token;

            // get the token
            try
            {
                token = _authenticator.GetToken();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
            
            // add token to the request and execute the request
            try
            {
                var uri = _settings.ToResourceUri(resource);
                var client = _client;
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                Logger.Info($"get request: {uri}");
                var response = await client.GetAsync(uri);

                return response;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Post Async request wrapper for making authenticated requests
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostAsync(string resource, StringContent json)
        {
            string token;

            // get the token
            try
            {
                token = _authenticator.GetToken();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
            
            // add token to the request and execute the request
            try
            {
                var uri = _settings.ToResourceUri(resource);
                
                var client = _client;
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PostAsync(uri, json);

                return response;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Put Async request wrapper for making authenticated requests
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutAsync(string resource, StringContent json)
        {
            string token;

            // get the token
            try
            {
                token = _authenticator.GetToken();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
            
            // add token to the request and execute the request
            try
            {
                var uri = _settings.ToResourceUri(resource);
                
                var client = _client;
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PutAsync(uri, json);

                return response;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Patch async wrapper for making authenticated requests
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PatchAsync(string resource, StringContent json)
        {
            string token;

            // get the token
            try
            {
                token = _authenticator.GetToken();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
            
            // add token to the request and execute the request
            try
            {
                var uri = _settings.ToResourceUri(resource);
                
                var client = _client;
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PatchAsync(uri, json);

                return response;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Delete async wrapper for making authenticated requests
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> DeleteAsync(string resource)
        {
            string token;

            // get the token
            try
            {
                token = _authenticator.GetToken();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
            
            // add token to the request and execute the request
            try
            {
                var uri = _settings.ToResourceUri(resource);
                
                var client = _client;
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);

                var response = await client.DeleteAsync(uri);

                return response;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }
    }
}