using System;
using System.IO;
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
using Utility.Serialization;
using Utility.Strings;
using Utility.Web;

namespace Datasilk
{
    public class Startup
    {
        protected static IConfigurationRoot config;
        protected Routes routes = new Routes();

        public virtual void ConfigureServices(IServiceCollection services)
        {
            //set up Server-side memory cache
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

            //add hsts
            services.AddHsts(options => { });
            services.AddHttpsRedirection(options =>{});

            //allow vendor to configure services
            ConfiguringServices(services);
        }

        public virtual void ConfiguringServices(IServiceCollection services) { }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //set root Server path
            var path = env.ContentRootPath + "\\";

            Server.RootPath = path;

            //get environment based on application build
            switch (env.EnvironmentName.ToLower())
            {
                case "production":
                    Server.environment = Server.Environment.production;
                    break;
                case "staging":
                    Server.environment = Server.Environment.staging;
                    break;
                default:
                    Server.environment = Server.Environment.development;
                    break;
            }

            //load application-wide cache
            var configFile = "config" + (Server.environment == Server.Environment.production ? ".prod" : "") + ".json";
            if (!File.Exists(Server.MapPath(configFile)))
            {
                //generate config file if none exists
                Serializer.WriteObjectToFile(new Models.Config(), Server.MapPath("config.json"));
            }
            config = new ConfigurationBuilder()
                .AddJsonFile(Server.MapPath(configFile))
                .AddEnvironmentVariables().Build();

            Server.config = config;

            //configure Server defaults
            Server.nameSpace = config.GetSection("assembly").Value;
            Server.defaultController = config.GetSection("defaultController").Value;
            Server.defaultServiceMethod = config.GetSection("defaultServiceMethod").Value;
            Server.hostUrl = config.GetSection("hostUrl").Value;
            var servicepaths = config.GetSection("servicePaths").Value;
            if (servicepaths != null && servicepaths != "")
            {
                Server.servicePaths = servicepaths.Replace(" ", "").Split(',');
            }
            if (config.GetSection("version").Value != null)
            {
                Server.Version = config.GetSection("version").Value;
            }

            //configure Server database connection strings
            Server.sqlActive = config.GetSection("sql:Active").Value;
            Server.sqlConnectionString = config.GetSection("sql:" + Server.sqlActive).Value;

            //configure Server security
            Server.bcrypt_workfactor = int.Parse(config.GetSection("Encryption:bcrypt_work_factor").Value);
            Server.salt = config.GetSection("Encryption:salt").Value;

            //configure Server Scaffold cache
            ScaffoldCache.cache = new Dictionary<string, SerializedScaffold>();

            //configure cookie-based authentication
            var expires = !string.IsNullOrEmpty(config.GetSection("Session:Expires").Value) ? int.Parse(config.GetSection("Session:Expires").Value) : 60;

            //use session
            var sessionOpts = new SessionOptions();
            sessionOpts.Cookie.Name = Server.nameSpace;
            sessionOpts.IdleTimeout = TimeSpan.FromMinutes(expires);

            app.UseSession(sessionOpts);

            //handle static files
            var provider = new FileExtensionContentTypeProvider();

            // Add static file mappings
            provider.Mappings[".svg"] = "image/svg";
            var options = new StaticFileOptions
            {
                ContentTypeProvider = provider
            };
            app.UseStaticFiles(options);

            //exception handling
            if (Server.environment == Server.Environment.development)
            {
                app.UseDeveloperExceptionPage(new DeveloperExceptionPageOptions
                {
                    SourceCodeLineCount = 10
                });
            }
            else
            {
                app.UseHsts();
            }
            //redirect to HTTPS
            app.UseHttpsRedirection();

            //Server if finished configuring
            Configured(app, env, config);

            //run Datasilk application
            app.Run(async (context) => await Run(context));
        }

        public virtual void Configured(IApplicationBuilder app, IWebHostEnvironment env, IConfigurationRoot config){}

        public virtual async Task Run(HttpContext context)
        {
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;
            var requestStart = DateTime.Now;
            DateTime requestEnd;
            TimeSpan tspan;
            var path = CleanPath(context.Request.Path.ToString());
            var paths = path.Split('/').ToArray();
            var isApiCall = false;

            Server.requestCount++;
            if(paths[paths.Length - 1].IndexOf(".") > 0)
            {
                //do not process files, but instead return a 404 error
                context.Response.StatusCode = 404;
                return;
            }

            if (Server.environment == Server.Environment.development)
            {
                Console.WriteLine("{0} " + context.Request.Method + " {1}", DateTime.Now.ToString("hh:mm:ss"), path);

                //optionally, wipe Scaffold cache to enable developer updates to html files when Server is running
                ScaffoldCache.cache = new Dictionary<string, SerializedScaffold>();
            }

            //get parameters from request body
            var parameters = await HeaderParameters.GetParameters(context);
            var requestBody = "";
            if (parameters.ContainsKey("_request-body"))
            {
                //extract request body from parameters
                requestBody = parameters["_request-body"];
                parameters.Remove("_request-body");
            }

            if (paths.Length > 1 && Server.servicePaths.Contains(paths[0]) == true)
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //run a web service via ajax (e.g. /api/namespace/class/function) //////////////////////////////////////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                //execute web service
                Server.apiRequestCount++;
                isApiCall = true;

                object[] paramVals;
                var param = "";

                //load service class from URL path
                context.Response.StatusCode = 200;
                string className = Server.nameSpace + ".Services." + paths[1].Replace("-","").ReplaceOnlyAlphaNumeric(true, true, false);
                string methodName = paths.Length > 2 ? paths[2] : Server.defaultServiceMethod;
                if (paths.Length >= 4) {
                    //path also contains extra namespace path(s)
                    for(var x = 2; x < paths.Length - 1; x++)
                    {
                        //add extra namespaces
                        className += "." + paths[x].Replace("-", "").ReplaceOnlyAlphaNumeric(true, true, false);
                    }
                    //get method name at end of path
                    methodName = paths[paths.Length - 1].Replace("-", "").ReplaceOnlyAlphaNumeric(true, true, false);
                }

                //get service type
                Type type = null;

                //get instance of service class
                var service = routes.FromServiceRoutes(context, parameters, className.Replace(Server.nameSpace + ".Services.", "").ToLower());
                if (service == null)
                {
                    try
                    {
                        service = (Web.Service)Activator.CreateInstance(Type.GetType(className), new object[] { context, parameters });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("service error");
                        return;
                    }
                }

                //check if service class was found
                type = service.GetType();
                if (type == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("service does not exist");
                    return;
                }

                //update service fields
                service.path = path;
                service.requestBody = requestBody;

                //get class method from service type
                MethodInfo method = type.GetMethod(methodName);

                //check if method exists
                if (method == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("service method " + methodName + " does not exist");
                    return;
                }

                //try to cast params to correct types
                ParameterInfo[] methodParams = method.GetParameters();

                paramVals = new object[methodParams.Length];
                for (var x = 0; x < methodParams.Length; x++)
                {
                    //find correct key/value pair
                    param = "";
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
                            paramVals[x] = (Dictionary<string, string>)Serializer.ReadObject(param, typeof(Dictionary<string, string>));
                        }
                        else if(paramType.Name == "Boolean")
                        {
                            paramVals[x] = param.ToLower() == "true";
                        }
                        else
                        {
                            //convert param value to matching method parameter type
                            paramVals[x] = Serializer.ReadObject(param, paramType);
                        }
                    }
                    else
                    {
                        //matching method parameter type is string
                        paramVals[x] = param;
                    }


                }

                string result = null;
                try
                {
                    //execute service method
                    result = (string)method.Invoke(service, paramVals);
                }
                catch (Exception ex)
                {
                    throw ex.InnerException;
                }
                if(context.Response.StatusCode == 200) {
                    //only write response if there were no errors

                    if(context.Response.ContentType == null)
                    {
                        //set content response as JSON
                        context.Response.ContentType = "text/json";
                    }
                    context.Response.ContentLength = result.Length;
                    if (result != null)
                    {
                        await context.Response.WriteAsync(result);
                    }
                    else
                    {
                        await context.Response.WriteAsync("{}");
                    }
                    service.Unload();
                }
            }
            else
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //page request (initialize client-side application) ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                Server.pageRequestCount++;

                //create instance of Controller class based on request URL path
                var html = "";
                var newpaths = path.Split('?', 2)[0].Split('/');
                var page = routes.FromControllerRoutes(context, parameters, newpaths[0].ToLower());

                if (page == null)
                {
                    //page is not part of any known routes, try getting page class manually
                    var typeName = (Server.nameSpace + ".Controllers." + (newpaths[0] == "" ? Server.defaultController : newpaths[0].Capitalize().Replace("-", " ")).Replace(" ", ""));
                    Type type = Type.GetType(typeName);
                    if(type == null)
                    {
                        throw new Exception("type " + typeName + " does not exist");
                    }
                    page = (Mvc.Controller)Activator.CreateInstance(type, new object[] { context, parameters });
                }

                if (page != null)
                {
                    //render page
                    try
                    {
                        page.path = path;
                        page.requestBody = requestBody;
                        html = page.Render(newpaths);
                    }
                    catch (Exception ex)
                    {
                        if (Server.environment == Server.Environment.development)
                        {
                            Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                        }
                        throw ex;
                    }
                }
                else
                {
                    //show 404 error
                    page = new Mvc.Controller(context, parameters);
                    html = page.Error404();
                }

                //unload Datasilk Core
                page.Unload();
                page = null;

                //send response back to client
                if (context.Response.ContentType == null ||
                    context.Response.ContentType == "")
                {
                    context.Response.ContentType = "text/html";
                }
                if (context.Response.HasStarted == false)
                {
                    await context.Response.WriteAsync(html);
                }
            }

            if (Server.environment == Server.Environment.development)
            {
                requestEnd = DateTime.Now;
                tspan = requestEnd - requestStart;
                Server.requestTime += (tspan.Seconds);
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
