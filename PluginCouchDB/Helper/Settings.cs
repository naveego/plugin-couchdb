using System;

namespace PluginCouchDB.Helper
{
    public class Settings
    {
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DatabaseName { get; set; }

        /// <summary>
        /// Validates the settings input object
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Validate()
        {
            if (String.IsNullOrEmpty(Hostname))
            {
                throw new Exception("the Hostname property must be set");
            }

            if (String.IsNullOrEmpty(Username))
            {
                throw new Exception("the Username property must be set");
            }

            if (String.IsNullOrEmpty(Password))
            {
                throw new Exception("the Password property must be set");
            }

            if (String.IsNullOrEmpty(DatabaseName))
            {
                throw new Exception("the database name must be set");
            }
        }

        /// <summary>
        /// Converts a resource to a resource uri
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public string ToResourceUri(string resource)
        {
            return String.Format("http://{0}:{1}@{2}/{3}", Username, Password, Hostname, resource.TrimStart('/'));
        }
    }
}