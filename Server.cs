using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Utility.Serialization;

public sealed class Server
{
    //create Server instance as singleton
    private Server() { }
    public static Server Instance { get; } = new Server();

    //environment
    public enum Environment
    {
        development = 0,
        staging = 1,
        production = 2
    }
    public Environment environment = Environment.development;

    //server properties
    public DateTime serverStart = DateTime.Now;
    public double requestCount = 0;
    public float requestTime = 0;

    //config properties
    public IConfiguration config;
    public string nameSpace = "";
    public string sqlActive = "";
    public string sqlConnectionString = "";
    public int bcrypt_workfactor = 10;
    public string salt = "";
    public bool hasAdmin = false; //no admin account exists
    public bool resetPass = false; //force admin to reset password
    public Dictionary<string, string> languages;

    private static string _path = "";

    //Dictionary used for caching non-serialized objects, files from disk, or raw text
    //be careful not to leak memory into the cache while causing an implosion!
    public Dictionary<string, object> Cache = new Dictionary<string, object>();

    //Dictionary used for HTML scaffolding of various files on the Server. 
    //Value for key/value pair is an array of HTML (scaffold["key"][x].htm), 
    //         separated by scaffold variable name (scaffold["key"][x].name),
    //         where data is injected in between each array item.
    public Dictionary<string, SerializedScaffold> Scaffold = new Dictionary<string, SerializedScaffold>();

    public static string MapPath(string strPath = "") {
        if (_path == "") { _path = Path.GetFullPath(".") + "\\"; }
        var str = strPath.Replace("/", "\\");
        if (str.Substring(0, 1) == "\\") { str = str.Substring(1); }
        return _path + str;
    }

    #region "Cache"
    /// <summary>
    /// Loads a file from cache. If the file hasn't been cached yet, then load file from a drive.
    /// </summary>
    /// <param name="filename">The relevant path to the file</param>
    /// <param name="noDevEnvCache">If true, it will not load a file from cache if the app is running in a development environment. Instead, it will always load the file from a drive.</param>
    /// <param name="noCache">If true, will not save to cache, but will instead load file from disk every time</param>
        /// <returns></returns>
    public string LoadFileFromCache(string filename, bool noDevEnvCache = false, bool noCache = false)
    {
        if ((environment != Environment.development || noDevEnvCache == false) && noCache == false)
        {
            //next, check cache
            if (Cache.ContainsKey(filename))
            {
                return (string)Cache[filename];
            }
        }
        if (File.Exists(MapPath(filename)))
        {
            //finally, check file system
            var file = File.ReadAllText(MapPath(filename));
            if (environment != Environment.development && noCache == false)
            {
                Cache.Add(filename, file);
            }
            return file;
        }
        return "";
    }

    public void SaveFileFromCache(string filename, string value)
    {
        File.WriteAllText(MapPath(filename), value);
        if (Cache.ContainsKey(filename))
        {
            Cache[filename] = value;
        }
        else
        {
            Cache.Add(filename, value);
        }
    }

    public void SaveToCache(string key, object value)
    {
        if (Cache.ContainsKey(key))
        {
            Cache[key] = value;
        }
        else
        {
            Cache.Add(key, value);
        }
    }

    public T LoadFromCache<T>(string key, Func<T> value, bool serialize = true)
    {
        if(Cache[key] == null)
        {
            var obj = value();
            SaveToCache(key, serialize ? (object)Serializer.WriteObjectToString(obj) : obj);
            return obj;
        }
        else
        {
            return serialize ? (T)Serializer.ReadObject((string)Cache[key], typeof(T)) : (T)Cache[key];
        }
    }
    #endregion
}
