using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
                client.DefaultRequestHeaders.Clear();
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                //Logger.Info($"get request: {uri}");
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
                client.DefaultRequestHeaders.Clear();
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
        /// Put Async request wrapper for adding database to couchDB
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutDataBaseAsync(string resource, StringContent json)
        {
            // add token to the request and execute the request
            try
            {
                var uri = String.Format("http://{0}/{1}", _settings.Hostname, resource);

                var client = _client;

                var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(
                    String.Format("{0}:{1}", _settings.Username, _settings.Password)));
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Logger.Info($"uri: {uri}, body: {await json.ReadAsStringAsync()}");
                var response = await client.PutAsync(uri, json);
                Logger.Info($"response: {await response.Content.ReadAsStringAsync()}");

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
                client.DefaultRequestHeaders.Clear();
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Logger.Info($"uri: {uri}, body: {await json.ReadAsStringAsync()}");
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
                client.DefaultRequestHeaders.Clear();
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
                client.DefaultRequestHeaders.Clear();
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