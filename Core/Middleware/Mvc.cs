using System;
using System.Web;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Datasilk.Core.Middleware
{
    public class Mvc
    {
        private RequestDelegate _next { get; set; }
        private readonly ILogger Logger;
        private readonly MvcOptions options;
        private int requestCount = 0;
        private Web.Routes routes;
        private Dictionary<string, Type> controllers = new Dictionary<string, Type>();
        private Dictionary<string, Type> services = new Dictionary<string, Type>();
        private Dictionary<string, string> controllerNamespaces = new Dictionary<string, string>();
        private Dictionary<string, string> serviceNamespaces = new Dictionary<string, string>();
        private Dictionary<string, MethodInfo> serviceMethods = new Dictionary<string, MethodInfo>();

        private string[] phishingPaths = new string[] {
            "phpmyadmin/", "webfig/", ".env", "config/getuser", "app", "shell", "boaform/admin/formLogin",
            "api/jsonws/invoke", "solr/", "CFIDE/administrator/", "latest/meta-data/", "solr/admin/info/system"
        };

        private string[] phishingParamKeys = new string[]
        {
            "XDEBUG_SESSION_START"
        };

        public Mvc(RequestDelegate next, MvcOptions options, ILoggerFactory loggerFactory)
        {
            _next = next;
            Logger = loggerFactory.CreateLogger<Mvc>();
            this.options = options;
            routes = this.options.Routes;
            var assemblies = new List<Assembly> { Assembly.GetCallingAssembly() };
            if (!assemblies.Contains(Assembly.GetExecutingAssembly()))
            {
                assemblies.Add(Assembly.GetExecutingAssembly());
            }
            if (!assemblies.Contains(Assembly.GetEntryAssembly()))
            {
                assemblies.Add(Assembly.GetEntryAssembly());
            }

            foreach (var assembly in assemblies)
            {
                //get a list of controllers from the assembly
                var types = assembly.GetTypes()
                    .Where(type => typeof(Web.IController).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract && type.Name != "Controller").ToList();
                foreach (var type in types)
                {
                    if (!type.Equals(typeof(Web.IController)))
                    {
                        controllers.Add((type.FullName).ToLower(), type);
                    }
                }

                types = assembly.GetTypes()
                    .Where(type => typeof(Web.IService).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract && type.Name != "Service").ToList();
                foreach (var type in types)
                {
                    if (!type.Equals(typeof(Web.IService)))
                    {
                        services.Add((type.FullName).ToLower(), type);
                    }
                }
            }

            if (options.LogRequests)
            {
                Logger.LogInformation("Datasilk Core MVC started ({0} controllers, {1} services)",
                    controllers.Count, services.Count);
            }
            if (options.WriteDebugInfoToConsole)
            {
                Console.WriteLine("Datasilk Core MVC started ({0} controllers, {1} services)",
                    controllers.Count, services.Count);
            }
        }

        public async Task Invoke(HttpContext context)
        {
            if (options.IgnoreRequestBodySize == true)
            {
                context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;
            }

            var requestStart = DateTime.Now;
            var path = CleanPath(context.Request.Path.ToString());

            //trap phishing requests that contain specific path values
            if (phishingPaths.Contains(path))
            {
                context.Response.StatusCode = 500;
                if (options.InvokeNext) { await _next.Invoke(context); }
                return;
            }

            var paths = path.Split('/').Where(a => a != "").ToArray();
            requestCount++;

            if (paths.Length > 0 && paths[^1].IndexOf(".") > 0)
            {
                //do not process files, but instead return a 404 error
                context.Response.StatusCode = 404;
                if (options.InvokeNext) { await _next.Invoke(context); }
                return;
            }
            //get parameters from request body
            var parameters = await GetParameters(context);

            //trap phishing requests that contain specific parameter keys
            if (phishingParamKeys.Any(a => parameters.Any(b => b.Key == a)))
            {
                context.Response.StatusCode = 500;
                if (options.InvokeNext) { await _next.Invoke(context); }
                return;
            }

            if (options.LogRequests)
            {
                Logger.LogDebug("{0} [{7}] {1} {2} ({3}), {4} kb, # {5}, params: {6}",
                    DateTime.Now.ToString("hh:mm:ss"),
                    context.Request.Method,
                    string.IsNullOrEmpty(path) ? "/" : path,
                    Math.Round(((DateTime.Now - requestStart)).TotalMilliseconds) + " ms",
                    ((parameters.RequestBody.Length * sizeof(char)) / 1024.0).ToString("N1"),
                    requestCount,
                    string.Join('&', parameters.Select(a => a.Key + "=" + a.Value).ToArray()),
                    context.Connection.RemoteIpAddress);
            }
            if (options.WriteDebugInfoToConsole)
            {
                Console.WriteLine("{0} [{7}] {1} {2} ({3}), {4} kb, # {5}, params: {6}",
                    DateTime.Now.ToString("hh:mm:ss"),
                    context.Request.Method,
                    string.IsNullOrEmpty(path) ? "/" : path,
                    Math.Round(((DateTime.Now - requestStart)).TotalMilliseconds) + " ms",
                    ((parameters.RequestBody.Length * sizeof(char)) / 1024.0).ToString("N1"),
                    requestCount,
                    string.Join('&', parameters.Select(a => a.Key + "=" + a.Value).ToArray()),
                    context.Connection.RemoteIpAddress);
            }

            if (paths.Length > 1 && options.ServicePaths.Contains(paths[0]) == true)
            {
                //handle web API requests
                ProcessService(context, path, paths, parameters);
            }
            else
            {
                //handle controller requests
                ProcessController(context, path, paths, parameters);
            }
            if (options.InvokeNext) { await _next.Invoke(context); }

        }

        private void ProcessController(HttpContext context, string path, string[] pathParts, Web.Parameters parameters)
        {
            var html = "";
            var newpaths = path.Split('?', 2)[0].Split('/');
            var page = routes.FromControllerRoutes(context, parameters, newpaths[0].ToLower());

            if (page == null)
            {
                //page is not part of any known routes, try getting page class manually
                var className = (newpaths[0] == "" ? options.DefaultController : newpaths[0].Replace("-", " ")).Replace(" ", "").ToLower();

                //get namespace from className
                var classNamespace = "";
                if (!controllerNamespaces.ContainsKey(className))
                {
                    //find namespace from compiled list of service namespaces
                    classNamespace = controllers.Keys.FirstOrDefault(a => a.Contains(className));
                    if (classNamespace != "")
                    {
                        controllerNamespaces.Add(className, classNamespace);
                    }
                    else
                    {
                        //could not find controller
                        page = new Web.Controller()
                        {
                            Context = context,
                            Parameters = parameters,
                            Path = path,
                            PathParts = pathParts
                        };
                        page.Init();
                        html = page.Error404();
                        return;
                    }
                }
                else
                {
                    classNamespace = controllerNamespaces[className];
                }
                //found controller
                page = (Web.IController)Activator.CreateInstance(controllers[classNamespace]);
            }

            if (page != null)
            {
                //render page
                page.Context = context;
                page.Parameters = parameters;
                page.Path = path;
                page.PathParts = pathParts;
                page.Init();
                html = page.Render();
            }
            else
            {
                //show 404 error
                page = new Web.Controller()
                {
                    Context = context,
                    Parameters = parameters,
                    Path = path,
                    PathParts = pathParts
                };
                page.Init();
                html = page.Error404();
            }

            //unload Datasilk Core
            page.Dispose();
            page = null;

            //send response back to client
            if (context.Response.ContentType == null ||
                context.Response.ContentType == "")
            {
                context.Response.ContentType = "text/html";
            }
            if (context.Response.HasStarted == false)
            {
                context.Response.WriteAsync(html);
            }
        }

        private void ProcessService(HttpContext context, string path, string[] pathParts, Web.Parameters parameters)
        {
            //load service class from URL path
            string className = CleanReflectionName(pathParts[1].Replace("-", "")).ToLower();
            string methodName = pathParts.Length > 2 ? pathParts[2] : options.DefaultServiceMethod;
            if (pathParts.Length >= 4)
            {
                //path also contains extra namespace path(s)
                for (var x = 2; x < pathParts.Length - 1; x++)
                {
                    //add extra namespaces
                    className += "." + CleanReflectionName(pathParts[x].Replace("-", "")).ToLower();
                }
                //get method name at end of path
                methodName = CleanReflectionName(pathParts[^1].Replace("-", ""));
            }

            //get service type
            Type type = null;

            //get instance of service class
            var service = routes.FromServiceRoutes(context, parameters, className);
            if (service == null)
            {
                //get namespace from className
                var classNamespace = "";
                if (!serviceNamespaces.ContainsKey(className))
                {
                    //find namespace from compiled list of service namespaces
                    classNamespace = services.Keys.FirstOrDefault(a => a.Contains(className));
                    if (classNamespace != "")
                    {
                        serviceNamespaces.Add(className, classNamespace);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        context.Response.WriteAsync("service does not exist");
                        return;
                    }
                }
                else
                {
                    classNamespace = serviceNamespaces[className];
                }
                service = (Web.IService)Activator.CreateInstance(services[classNamespace]);
            }

            //check if service class was found
            type = service.GetType();
            if (type == null)
            {
                context.Response.StatusCode = 404;
                context.Response.WriteAsync("service does not exist");
                return;
            }

            //update service fields
            service.Context = context;
            service.Parameters = parameters;
            service.Path = path;
            service.PathParts = pathParts;
            service.Init();
            if (context.Response.StatusCode >= 400)
            {
                //service init returned an error status code
                return;
            }

            //get class method from service type
            var serviceMethodName = className + "/" + methodName;
            MethodInfo method;
            if (serviceMethods.ContainsKey(serviceMethodName))
            {
                method = serviceMethods[serviceMethodName];
            }
            else
            {
                method = type.GetMethod(methodName);
                serviceMethods.Add(serviceMethodName, method);
            }

            //check if method exists
            if (method == null)
            {
                context.Response.StatusCode = 404;
                context.Response.WriteAsync("Web service method " + methodName + " does not exist");
                return;
            }

            //check request method
            if (!CanUseRequestMethod(context, method))
            {
                context.Response.StatusCode = 400;
                context.Response.WriteAsync("Web service method " + methodName + " does not accept the '" + context.Request.Method + "' request method");
                return;
            }

            //try to cast params to correct types
            var paramVals = MapParameters(method.GetParameters(), parameters, method);

            //execute service method
            string result = (string)method.Invoke(service, paramVals);
            service.Dispose();

            if (context.Response.HasStarted == false)
            {
                if (context.Response.ContentType == null)
                {
                    if (result.IndexOf("{") < 0)
                    {
                        context.Response.ContentType = "text/plain";
                    }
                    else if (result.IndexOf("{") >= 0 && result.IndexOf("}") > 0)
                    {
                        context.Response.ContentType = "text/json";
                    }
                }
                //context.Response.ContentLength = result.Length;
                if (result != null)
                {
                    context.Response.WriteAsync(result);
                }
                else
                {
                    context.Response.WriteAsync("{}");
                }
            }
        }

        #region "Helpers"
        private string CleanPath(string path)
        {
            //check for malicious path input
            if (path == "") { return path; }
            if (path[0] == '/') { path = path.Substring(1); }
            if (path.Replace("/", "").Replace("-", "").Replace("+", "").All(char.IsLetterOrDigit))
            {
                //path is clean
                return path;
            }

            //path needs to be cleaned
            return path
                .Replace("{", "")
                .Replace("}", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace(":", "")
                .Replace("$", "")
                .Replace("!", "")
                .Replace("*", "");
        }

        private async static Task<Web.Parameters> GetParameters(HttpContext context)
        {
            var contentType = context.Request.ContentType;
            var parameters = new Web.Parameters();
            string data = "";
            if (contentType != null && contentType.IndexOf("multipart/form-data") >= 0)
            {
                GetMultipartParameters(context, parameters, Encoding.UTF8);
            }
            else if (context.Request.ContentType == "application/octet-stream")
            {
                //file uploaded via HTML 5 ajax or similar method
                var filename = context.Request.Headers["X-File-Name"].ToString();
                var formFile = new Web.FormFile()
                {
                    Filename = filename,
                    ContentType = contentType
                };
                await context.Request.Body.CopyToAsync(formFile);
                formFile.Seek(0, SeekOrigin.Begin);
                parameters.Files.Add(filename, formFile);
            }
            else if (contentType != null && contentType.IndexOf("multipart/form-data") < 0 && context.Request.Body.CanRead)
            {
                //get POST data from request
                byte[] bytes = new byte[0];
                using (MemoryStream ms = new MemoryStream())
                {
                    await context.Request.Body.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
                data = Encoding.UTF8.GetString(bytes, 0, bytes.Length).Trim();
            }


            if (data.Length > 0)
            {
                parameters.RequestBody = data;
                if (data.IndexOf("Content-Disposition") < 0 && (
                    (data.IndexOf("{") == 0 || data.IndexOf("[") == 0) && data.IndexOf(":") > 0)
                )
                {
                    //get method parameters from POST
                    Dictionary<string, object> attr = JsonSerializer.Deserialize<Dictionary<string, object>>(data);
                    foreach (KeyValuePair<string, object> item in attr)
                    {
                        var key = WebUtility.UrlDecode(item.Key.ToLower());
                        if (parameters.ContainsKey(key))
                        {
                            parameters.Remove(parameters[key]);
                        }
                        if (item.Value != null)
                        {
                            parameters.Add(key, item.Value.ToString());
                        }
                        else
                        {
                            parameters.Add(key, "");
                        }
                    }
                }
                else if (contentType != null && contentType.IndexOf("application/x-www-form-urlencoded") >= 0)
                {
                    var kvps = data.Split("&");
                    foreach (var kv in kvps)
                    {
                        var kvp = kv.Split("=");
                        parameters.Add(kvp[0], kvp.Length > 1 ? HttpUtility.UrlDecode(kvp[1]) : "");
                    }
                }
            }

            //get method parameters from query string
            foreach (var key in context.Request.Query.Keys)
            {
                var value = context.Request.Query[key].ToString();
                if (!parameters.ContainsKey(key))
                {
                    parameters.Add(key, HttpUtility.UrlDecode(value));
                }
                else
                {
                    parameters.AddTo(key, HttpUtility.UrlDecode(value));
                }
            }
            return parameters;
        }


        private static void GetMultipartParameters(HttpContext context, Web.Parameters parameters, Encoding encoding)
        {
            var data = ToByteArray(context.Request.BodyReader.AsStream());
            var content = encoding.GetString(data);
            int delimiterEndIndex = content.IndexOf("\r\n");

            if (delimiterEndIndex > -1)
            {
                var delimiter = content.Substring(0, content.IndexOf("\r\n"));
                var sections = content.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
                var delimiterBytes = encoding.GetBytes("\r\n" + delimiter);
                var totalLength = delimiter.Length;

                foreach (string s in sections)
                {
                    // if we find "Content-Disposition", this is a valid multi-part section
                    if (s.Contains("Content-Disposition"))
                    {
                        // look for the "name" parameter
                        Match nameMatch = new Regex(@"(?<=name\=\"")(.*?)(?=\"")").Match(s);
                        if (nameMatch.Success == false)
                        {
                            nameMatch = new Regex(@"(?<=name\=)(.*?)(?=\r\n)").Match(s);
                        }
                        string name = nameMatch.Value.Trim().ToLower();
                        if (name.IndexOf(";") > 0)
                        {
                            nameMatch = new Regex(@"(?<=name\=)(.*?)(?=;)").Match(s);
                            name = nameMatch.Value.Trim().ToLower();
                        }

                        // look for Content-Type
                        Match contentTypeMatch = new Regex(@"(?<=Content\-Type:)(.*?)(?=\r\n\r\n)").Match(s);
                        if (contentTypeMatch.Success == false)
                        {
                            contentTypeMatch = new Regex(@"(?<=Content\-Type:)(.*?)(?=;)").Match(s);
                        }
                        if (contentTypeMatch.Success == false)
                        {
                            contentTypeMatch = new Regex(@"(?<=Content\-Type:)(.*?)(?=\r\n)").Match(s);
                        }

                        // look for Content-Disposition
                        Match contentDispositionMatch = new Regex(@"(?<=Content\-Type:)(.*?)(?=\r\n\r\n)").Match(s);
                        if (contentDispositionMatch.Success == false)
                        {
                            contentDispositionMatch = new Regex(@"(?<=Content\-Type:)(.*?)(?=;)").Match(s);
                        }
                        if (contentDispositionMatch.Success == false)
                        {
                            contentDispositionMatch = new Regex(@"(?<=Content\-Type:)(.*?)(?=\r\n)").Match(s);
                        }

                        // look for filename
                        Match filenameMatch = new Regex(@"(?<=filename\=\"")(.*?)(?=\"")").Match(s);
                        if (filenameMatch.Success == false)
                        {
                            filenameMatch = new Regex(@"(?<=filename\=)(.*?)(?=;)").Match(s);
                        }

                        // look for filename*
                        Match filenameWildMatch = new Regex(@"(?<=filename\*\=\"")(.*?)(?=\"")").Match(s);
                        if (filenameWildMatch.Success == false)
                        {
                            filenameWildMatch = new Regex(@"(?<=filename\*\=)(.*?)(?=;)").Match(s);
                        }

                        // did we find the required values?
                        if (contentTypeMatch.Success && filenameMatch.Success)
                        {
                            // get the start & end indexes of the file contents
                            var maxIndex = Math.Max(Math.Max(Math.Max(
                                contentTypeMatch.Index,
                                contentDispositionMatch.Index),
                                filenameMatch.Index),
                                filenameWildMatch.Index);
                            Match propsEnd = new Regex(@"(?=\r\n\r\n)").Match(s, maxIndex);
                            Match propsEnd2 = new Regex(@"(?=\r\n)").Match(s, maxIndex);
                            var propsEndIndex = 0;
                            if (propsEnd2.Index == propsEnd.Index)
                            {
                                propsEndIndex = propsEnd.Index + 4;
                            }
                            else
                            {
                                propsEndIndex = propsEnd2.Index + 2;
                            }
                            var startIndex = totalLength + propsEndIndex;
                            var endIndex = IndexOf(data, delimiterBytes, startIndex);
                            var contentLength = endIndex - startIndex;

                            // extract the file contents from the byte array
                            var fileData = new byte[contentLength];
                            Buffer.BlockCopy(data, startIndex, fileData, 0, contentLength);

                            //create form file
                            var formFile = new Web.FormFile()
                            {
                                Filename = filenameMatch.Value.Trim(),
                                ContentType = contentTypeMatch.Value.Trim()
                            };
                            formFile.Write(fileData, 0, contentLength);
                            formFile.Seek(0, SeekOrigin.Begin);

                            //add form file to Files list
                            parameters.Files.Add(name, formFile);
                            totalLength = endIndex + delimiterBytes.Length;
                        }
                        else if (!string.IsNullOrWhiteSpace(name))
                        {
                            // Get the start & end indexes of the file contents
                            int startIndex = nameMatch.Index + nameMatch.Length + 4; // "\r\n\r\n".Length;
                            parameters.Add(name, HttpUtility.UrlDecode(s.Substring(startIndex).TrimEnd(new char[] { '\r', '\n' }).Trim()));
                            totalLength += s.Length + delimiter.Length;
                        }
                    }
                }

            }
        }

        private static object[] MapParameters(ParameterInfo[] methodParams, Web.Parameters parameters, MethodInfo method)
        {
            var paramVals = new object[methodParams.Length];
            for (var x = 0; x < methodParams.Length; x++)
            {
                //find correct key/value pair
                var param = "";
                var methodParamName = methodParams[x].Name.ToLower();
                var paramType = methodParams[x].ParameterType;

                foreach (var item in parameters)
                {
                    if (item.Key == methodParamName)
                    {
                        param = item.Value;
                        break;
                    }
                }

                if (param == "")
                {
                    //set default value for empty parameter
                    if (paramType == typeof(Int32))
                    {
                        param = "0";
                    }
                }

                //cast params to correct (supported) types
                if (paramType.Name != "String")
                {
                    if (int.TryParse(param, out int i) == true)
                    {
                        if (paramType.IsEnum == true)
                        {
                            //convert param value to enum
                            paramVals[x] = Enum.Parse(paramType, param);
                        }
                        else
                        {
                            //convert param value to matching method parameter number type
                            paramVals[x] = Convert.ChangeType(i, paramType);
                        }

                    }
                    else if (paramType.FullName.Contains("DateTime"))
                    {
                        //convert param value to DateTime
                        if (param == "")
                        {
                            paramVals[x] = null;
                        }
                        else
                        {
                            try
                            {
                                paramVals[x] = DateTime.Parse(param);
                            }
                            catch (Exception) { }
                        }
                    }
                    else if (paramType.IsArray)
                    {
                        //convert param value to array (of T)
                        var arr = param.Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Split(",").Select(a => { return a.Trim(); }).ToList();
                        if (paramType.FullName == "System.Int32[]")
                        {
                            //convert param values to int array
                            paramVals[x] = arr.Select(a => { return int.Parse(a); }).ToArray();
                        }
                        else
                        {
                            //convert param values to array (of matching method parameter type)
                            paramVals[x] = Convert.ChangeType(arr, paramType);
                        }


                    }
                    else if (paramType.Name.IndexOf("Dictionary") == 0)
                    {
                        //convert param value (JSON) to Dictionary
                        if (param != "" && param != "[]" && param != "{}")
                        {
                            try
                            {
                                paramVals[x] = JsonSerializer.Deserialize<Dictionary<string, string>>(param);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Could not convert JSON string into Dictionary for parameter \"" + methodParamName + "\" in method \"" + method.Name + "\"");
                            }
                        }

                        if (paramVals[x] == null)
                        {
                            paramVals[x] = new Dictionary<string, string>();
                        }

                    }
                    else if (paramType.Name == "Boolean")
                    {
                        paramVals[x] = param.ToLower() == "true";
                    }
                    else
                    {
                        //convert param value to matching method parameter type
                        try
                        {
                            paramVals[x] = JsonSerializer.Deserialize(param, paramType);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                        }
                    }
                }
                else
                {
                    //matching method parameter type is string
                    paramVals[x] = param;
                }
            }
            return paramVals;
        }

        private static string CleanReflectionName(string myStr)
        {
            string newStr = myStr.ToString();
            int x = 0;
            while (x < newStr.Length)
            {
                if (
                        (Encoding.ASCII.GetBytes(newStr.Substring(x, 1))[0] >= Encoding.ASCII.GetBytes("a")[0] && Encoding.ASCII.GetBytes(newStr.Substring(x, 1))[0] <= Encoding.ASCII.GetBytes("z")[0]) ||
                        (Encoding.ASCII.GetBytes(newStr.Substring(x, 1))[0] >= Encoding.ASCII.GetBytes("A")[0] & Encoding.ASCII.GetBytes(newStr.Substring(x, 1))[0] <= Encoding.ASCII.GetBytes("Z")[0]) ||
                        (Encoding.ASCII.GetBytes(newStr.Substring(x, 1))[0] >= Encoding.ASCII.GetBytes("0")[0] & Encoding.ASCII.GetBytes(newStr.Substring(x, 1))[0] <= Encoding.ASCII.GetBytes("9")[0])
                    )
                {
                    x++;
                }
                else
                {
                    //remove character
                    newStr = newStr.Substring(0, x - 1) + newStr.Substring(x + 1);
                }
            }
            return newStr;
        }

        private static bool CanUseRequestMethod(HttpContext context, MethodInfo method)
        {
            var reqMethod = context.Request.Method.ToLower();
            var hasReqAttr = false;
            switch (reqMethod)
            {
                case "get": hasReqAttr = method.GetCustomAttributes(typeof(Web.GETAttribute), false).Any(); break;
                case "post": hasReqAttr = method.GetCustomAttributes(typeof(Web.POSTAttribute), false).Any(); break;
                case "put": hasReqAttr = method.GetCustomAttributes(typeof(Web.PUTAttribute), false).Any(); break;
                case "head": hasReqAttr = method.GetCustomAttributes(typeof(Web.HEADAttribute), false).Any(); break;
                case "delete": hasReqAttr = method.GetCustomAttributes(typeof(Web.DELETEAttribute), false).Any(); break;
            }
            if (hasReqAttr == false)
            {
                //check if method contains other request method attributes
                if ((method.GetCustomAttributes(typeof(Web.GETAttribute), false).Any() && reqMethod != "get") ||
                    (method.GetCustomAttributes(typeof(Web.POSTAttribute), false).Any() && reqMethod != "post") ||
                    (method.GetCustomAttributes(typeof(Web.PUTAttribute), false).Any() && reqMethod != "put") ||
                    (method.GetCustomAttributes(typeof(Web.HEADAttribute), false).Any() && reqMethod != "head") ||
                    (method.GetCustomAttributes(typeof(Web.DELETEAttribute), false).Any() && reqMethod != "delete"))
                {
                    //display an error
                    return false;
                }
            }
            return true;
        }

        private static int IndexOf(byte[] searchWithin, byte[] serachFor, int startIndex)
        {
            int index = 0;
            int startPos = Array.IndexOf(searchWithin, serachFor[0], startIndex);

            if (startPos != -1)
            {
                while ((startPos + index) < searchWithin.Length)
                {
                    if (searchWithin[startPos + index] == serachFor[index])
                    {
                        index++;
                        if (index == serachFor.Length)
                        {
                            return startPos;
                        }
                    }
                    else
                    {
                        startPos = Array.IndexOf<byte>(searchWithin, serachFor[0], startPos + index);
                        if (startPos == -1)
                        {
                            return -1;
                        }
                        index = 0;
                    }
                }
            }

            return -1;
        }

        private static byte[] ToByteArray(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }
        #endregion
    }
}
