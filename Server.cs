using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

public class Server
{
    ////////////////////////////////////////////////
    //Server     (for application-wide memory store)
    ////////////////////////////////////////////////

    public enum enumEnvironment
    {
        development = 0,
        staging = 1,
        production = 2
    }

    public string Version = "0.3";
    public Utility.Util Util = new Utility.Util();
    public enumEnvironment environment = enumEnvironment.development;
    public DateTime serverStart = DateTime.Now;
    public int requestCount = 0;
    public float requestTime = 0;
    public string nameSpace = "";
    public string sqlActive = "";
    public string sqlConnectionString = "";
    public Random Random = new Random();
    public IConfiguration config;
    public int bcrypt_workfactor = 10;
    public string salt = "";
    public bool hasAdmin = false; //if set to true, admin exists
    public bool resetPass = false; //if set to true, require admin password to be reset 

    private string _path = "";

    //Dictionary used for caching non-serialized objects, files from disk, or raw text
    //be careful not to leak memory into the cache while causing an implosion!
    public Dictionary<string, object> Cache = new Dictionary<string, object>();

    //Dictionary used for HTML scaffolding of various files on the server. 
    //Value for key/value pair is an array of HTML (scaffold["key"][x].htm), 
    //         separated by scaffold variable name (scaffold["key"][x].name),
    //         where data is injected in between each array item.
    public Dictionary<string, structScaffold> Scaffold = new Dictionary<string, structScaffold>();

    #region "System.UI.Web.Page.Server methods"
    public string MapPath(string strPath = "") {
        if (_path == "") { _path = Path.GetFullPath(".") + "\\"; }
        var str = strPath.Replace("/", "\\");
        if (str.Substring(0, 1) == "\\") { str = str.Substring(1); }
        return _path + str;
    }

    public string UrlDecode(string strPath)
    {
        return Uri.UnescapeDataString(strPath);
    }

    public string UrlEncode(string strPath)
    {
        return Uri.EscapeDataString(strPath);
    }

    #endregion

    #region "Cache"
    /// <summary>
    /// Loads a file from cache. If the file hasn't been cached yet, then load file from a drive.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="noDevEnvCache">If true, it will not load a file from cache if the app is running in a development environment. Instead, it will always load the file from a drive.</param>
    /// <param name="noCache">If true, will not save to cache, but will instead load file from disk every time</param>
        /// <returns></returns>
    public string LoadFileFromCache(string filename, bool noDevEnvCache = false, bool noCache = false)
    {
        if ((environment != enumEnvironment.development || noDevEnvCache == false) && noCache == false)
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
            if (environment != enumEnvironment.development && noCache == false)
            {
                Cache.Add(filename, file);
            }
            return file;
        }
        return "";
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
            SaveToCache(key, serialize ? (object)Util.Serializer.WriteObjectToString(obj) : obj);
            return obj;
        }
        else
        {
            return serialize ? (T)Util.Serializer.ReadObject((string)Cache[key], typeof(T)) : (T)Cache[key];
        }
    }
    #endregion
}
