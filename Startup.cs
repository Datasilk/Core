using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Utility.Serialization;
using Utility.Strings;

namespace Datasilk
{
    public class Startup
    {
        protected Server server = Server.Instance;
        protected static IConfigurationRoot config;
        protected global::Routes routes = new global::Routes();

        public virtual void ConfigureServices(IServiceCollection services)
        {
            //set up server-side memory cache
            services.AddDistributedMemoryCache();
            services.AddMemoryCache();

            //configure request form options
            services.Configure<FormOptions>(x => 
                {
                    x.ValueLengthLimit = int.MaxValue;
                    x.MultipartBodyLengthLimit = int.MaxValue;
                    x.MultipartHeadersLengthLimit = int.MaxValue;
                }
            );
            
            //add session
            services.AddSession();

            //allow vendor to configure services
            ConfiguringServices(services);
        }

        public virtual void ConfiguringServices(IServiceCollection services) { }

        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //set root server path
            var path = env.WebRootPath.Replace("wwwroot", "");
            server.RootPath = path;

            //load application-wide cache
            if (!File.Exists(Server.MapPath("config.json")))
            {
                //generate config file if none exists
                Serializer.WriteObjectToFile(new Models.Config(), Server.MapPath("config.json"));
            }
            config = new ConfigurationBuilder()
                .AddJsonFile(Server.MapPath("config.json"))
                .AddEnvironmentVariables().Build();

            server.config = config;

            //configure server defaults
            server.nameSpace = config.GetSection("assembly").Value;
            server.defaultController = config.GetSection("defaultController").Value;
            server.defaultServiceMethod = config.GetSection("defaultServiceMethod").Value;
            var servicepaths = config.GetSection("servicePaths").Value;
            if(servicepaths != "")
            {
                server.servicePaths = servicepaths.Replace(" ", "").Split(',');
            }

            //configure server database connection strings
            server.sqlActive = config.GetSection("sql:Active").Value;
            server.sqlConnectionString = config.GetSection("sql:" + server.sqlActive).Value;

            //configure server environment
            switch (config.GetSection("environment").Value.ToLower())
            {
                case "development":
                case "dev":
                    server.environment = Server.Environment.development;
                    break;
                case "staging":
                case "stage":
                    server.environment = Server.Environment.staging;
                    break;
                case "production":
                case "prod":
                    server.environment = Server.Environment.production;
                    break;
            }

            //configure server security
            server.bcrypt_workfactor = int.Parse(config.GetSection("Encryption:bcrypt_work_factor").Value);
            server.salt = config.GetSection("Encryption:salt").Value;

            //configure server Scaffold cache
            ScaffoldCache.cache = Server.Scaffold;

            //configure cookie-based authentication
            var expires = !string.IsNullOrEmpty(config.GetSection("Session:Expires").Value) ? int.Parse(config.GetSection("Session:Expires").Value) : 60;

            //use session
            var sessionOpts = new SessionOptions();
            sessionOpts.Cookie.Name = server.nameSpace;
            sessionOpts.IdleTimeout = TimeSpan.FromMinutes(expires);

            app.UseSession(sessionOpts);

            //handle static files
            var provider = new FileExtensionContentTypeProvider();

            // Add new mappings
            provider.Mappings[".svg"] = "image/svg";
            var options = new StaticFileOptions
            {
                ContentTypeProvider = provider
            };
            app.UseStaticFiles(options);

            //exception handling
            var errOptions = new DeveloperExceptionPageOptions
            {
                SourceCodeLineCount = 10
            };
            app.UseDeveloperExceptionPage();

            //server if finished configuring
            Configured(app, env, config);

            //run Datasilk application
            app.Run(async (context) => await Run(context));
        }

        public virtual void Configured(IApplicationBuilder app, IHostingEnvironment env, IConfigurationRoot config){}

        public virtual async Task Run(HttpContext context)
        {
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;
            var requestStart = DateTime.Now;
            DateTime requestEnd;
            TimeSpan tspan;
            var path = CleanPath(context.Request.Path.ToString());
            var paths = path.Split('/').ToArray();
            var isApiCall = false;

            server.requestCount++;
            if(paths[paths.Length - 1].IndexOf(".") > 0)
            {
                //do not process files, but instead return a 404 error
                context.Response.StatusCode = 404;
                return;
            }

            if (server.environment == Server.Environment.development)
            {
                Console.WriteLine("{0} GET {1}", DateTime.Now.ToString("hh:mm:ss"), path);

                //optionally, wipe Scaffold cache to enable developer updates to html files when server is running
                Server.Scaffold = new Dictionary<string, SerializedScaffold>();
            }

            if (paths.Length > 1 && server.servicePaths.Contains(paths[0]) == true)
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //run a web service via ajax (e.g. /api/namespace/class/function) //////////////////////////////////////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                //execute web service
                server.apiRequestCount++;
                isApiCall = true;

                //get parameters from request body, including page id
                var parms = new Dictionary<string, string>();
                object[] paramVals;
                var param = "";
                string data = "";
                if (context.Request.ContentType != null && context.Request.ContentType.IndexOf("multipart/form-data") < 0 && context.Request.Body.CanRead)
                {
                    //get POST data from request
                    byte[] bytes = new byte[0];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        context.Request.Body.CopyTo(ms);
                        bytes = ms.ToArray();
                    }
                    data = Encoding.UTF8.GetString(bytes, 0, bytes.Length).Trim();
                }

                if (data.Length > 0)
                {
                    if (data.IndexOf("Content-Disposition") < 0 && data.IndexOf("{") >= 0 && data.IndexOf("}") > 0 && data.IndexOf(":") > 0)
                    {
                        //get method parameters from POST S.ajax.post()
                        Dictionary<string, object> attr = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
                        foreach (KeyValuePair<string, object> item in attr)
                        {
                            parms.Add(item.Key.ToLower(), item.Value.ToString());
                        }
                    }
                }
                else
                {
                    //get method parameters from query string
                    foreach (var key in context.Request.Query.Keys)
                    {
                        parms.Add(key.ToLower(), context.Request.Query[key].ToString());
                    }
                }

                //load service class from URL path
                context.Response.StatusCode = 200;
                string className = server.nameSpace + ".Services." + paths[1].Replace("-","").Replace(" ", "");
                string methodName = paths.Length > 2 ? paths[2] : server.defaultServiceMethod;
                if (paths.Length == 4) { className += "." + paths[2]; methodName = paths[3]; }
                var service = routes.FromServiceRoutes(context, className);
                if (service == null)
                {
                    try
                    {
                        service = (Web.Service)Activator.CreateInstance(Type.GetType(className), new object[] { context });
                    }
                    catch (Exception) { }
                }

                //check if service class was found
                if (service == null)
                {
                    throw new Exception("no service found");
                }

                //execute method from new Service instance
                Type type = Type.GetType(className);
                if (type == null)
                {
                    throw new Exception("type " + className + " does not exist");
                }
                MethodInfo method = type.GetMethod(methodName);

                //try to cast params to correct types
                ParameterInfo[] methodParams = method.GetParameters();

                paramVals = new object[methodParams.Length];
                for (var x = 0; x < methodParams.Length; x++)
                {
                    //find correct key/value pair
                    param = "";
                    foreach (var item in parms)
                    {
                        if (item.Key == methodParams[x].Name.ToLower())
                        {
                            param = item.Value;
                            break;
                        }
                    }

                    if (param == "")
                    {
                        //set default value for empty parameter
                        var t = methodParams[x].ParameterType;
                        if (t == typeof(Int32))
                        {
                            param = "0";
                        }
                    }

                    //cast params to correct (supported) types
                    if (methodParams[x].ParameterType.Name != "String")
                    {
                        if (int.TryParse(param, out int i) == true)
                        {
                            if (methodParams[x].ParameterType.IsEnum == true)
                            {
                                //enum
                                paramVals[x] = Enum.Parse(methodParams[x].ParameterType, param);
                            }
                            else
                            {
                                //int
                                paramVals[x] = Convert.ChangeType(i, methodParams[x].ParameterType);
                            }

                        }
                        else if (methodParams[x].ParameterType.FullName.Contains("DateTime"))
                        {
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
                        else if (methodParams[x].ParameterType.IsArray)
                        {
                            var arr = param.Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Split(",").Select(a => { return a.Trim(); }).ToList();
                            if (methodParams[x].ParameterType.FullName == "System.Int32[]")
                            {
                                paramVals[x] = arr.Select(a => { return int.Parse(a); }).ToArray();
                            }
                            else
                            {
                                paramVals[x] = Convert.ChangeType(arr, methodParams[x].ParameterType);
                            }


                        }
                        else if (methodParams[x].ParameterType.Name.IndexOf("Dictionary") == 0)
                        {
                            paramVals[x] = (Dictionary<string, string>)Serializer.ReadObject(param, typeof(Dictionary<string, string>));
                        }
                        else
                        {
                            paramVals[x] = Convert.ChangeType(param, methodParams[x].ParameterType);
                        }
                    }
                    else
                    {
                        //string
                        paramVals[x] = param;
                    }


                }

                object result = null;
                try
                {
                    result = method.Invoke(service, paramVals);
                }
                catch (Exception ex)
                {
                    if (server.environment == Server.Environment.development)
                    {
                        Console.WriteLine(ex.InnerException.Message + "\n" + ex.InnerException.StackTrace);
                    }
                    throw ex.InnerException;
                }
                if(context.Response.StatusCode == 200) {
                    //finally, unload the service
                    service.Unload();

                    //set content response as JSON
                    if(context.Response.ContentType == null)
                    {
                        context.Response.ContentType = "text/json";
                    }
                    if (result != null)
                    {
                        await context.Response.WriteAsync((string)result);
                    }
                    else
                    {
                        await context.Response.WriteAsync("{}");
                    }
                }
            }
            else
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //page request (initialize client-side application) ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                server.pageRequestCount++;

                //create instance of Controller class based on request URL path
                var html = "";
                var newpaths = path.Split('?', 2)[0].Split('/');
                var page = routes.FromControllerRoutes(context, newpaths[0].ToLower());

                if (page == null)
                {
                    //page is not part of any known routes, try getting page class manually
                    var typeName = (server.nameSpace + ".Controllers." + (newpaths[0] == "" ? server.defaultController : newpaths[0].Capitalize().Replace("-", " ")).Replace(" ", ""));
                    Type type = Type.GetType(typeName);
                    if(type == null)
                    {
                        throw new Exception("type " + typeName + " does not exist");
                    }
                    page = (Mvc.Controller)Activator.CreateInstance(type, new object[] { context });
                }

                if (page != null)
                {
                    //render page
                    try
                    {
                        html = page.Render(newpaths);
                    }
                    catch (Exception ex)
                    {
                        if (server.environment == Server.Environment.development)
                        {
                            Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                        }
                        throw ex;
                    }
                }
                else
                {
                    //show 404 error
                    page = new Mvc.Controller(context);
                    html = page.Error404();
                }

                //unload Datasilk Core
                page.Unload();
                page = null;

                //send response back to client
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(html);
            }

            if (server.environment == Server.Environment.development)
            {
                requestEnd = DateTime.Now;
                tspan = requestEnd - requestStart;
                server.requestTime += (tspan.Seconds);
                Console.WriteLine("END REQUEST {0} ms, {1} {2}", tspan.Milliseconds, path, isApiCall ? "Service" : "Controller");
                Console.WriteLine("");
            }
        }

        #region "Utility"

        private string CleanPath(string path)
        {
            //check for malicious path input
            if(path == "") { return path; }
            if(path[0] == '/') { path = path.Substring(1); }
            if (path.Replace("/", "").Replace("-","").Replace("+","").All(char.IsLetterOrDigit)) {
                //path is clean
                return path;
            }

            //path needs to be cleaned
            return path
                .Replace("{", "")
                .Replace("}", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace(":","")
                .Replace("$","")
                .Replace("!","")
                .Replace("*","");
        }

        private static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="key";
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && string.IsNullOrEmpty(contentDisposition.FileName.ToString())
                   && string.IsNullOrEmpty(contentDisposition.FileNameStar.ToString());
        }

        private static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && (!string.IsNullOrEmpty(contentDisposition.FileName.ToString())
                       || !string.IsNullOrEmpty(contentDisposition.FileNameStar.ToString()));
        }

        #endregion
    }
}
