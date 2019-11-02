using System.Collections.Generic;

namespace Datasilk.Core.Web
{
    /// <summary>
    /// Parameters extracted from the request body
    /// </summary>
    public class Parameters : Dictionary<string, string>
    {
        /// <summary>
        /// Raw data extracted from the request body before deserialized into parameters
        /// </summary>
        public string RequestBody { get; set; } = "";
        private List<string> _isArray = new List<string>();


        /// <summary>
        /// Adds a value (comma-delimited) to an existing key/value pair
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddTo(string key, string value)
        {
            _isArray.Add(key);
            var param = this[key];
            this[key] = param + "^,^" + value;
        }

        /// <summary>
        /// Return whether or not a specific key has a comma-delimited set of values
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsArray(string key)
        {
            return _isArray.Contains(key);
        }

        /// <summary>
        /// Gets a list of values for a specified key in the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string[] GetValues(string key)
        {
            return this[key].Split("^,^");
        }
    }
}

