using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace Datasilk
{
    public class Startup
    {
        protected static Server server;
        protected static IConfigurationRoot config;

        public virtual void ConfigureServices(IServiceCollection services)
        {
            //load application-wide cache
            server = new Server();

            config = new ConfigurationBuilder()
                .AddJsonFile(server.MapPath("config.json"))
                .AddEnvironmentVariables().Build();

            server.config = config;

            server.nameSpace = config.GetSection("Namespace").Value;
            server.sqlActive = config.GetSection("Data:Active").Value;
            server.sqlConnectionString = config.GetSection("Data:" + server.sqlActive).Value;

            switch (config.GetSection("Environment").Value.ToLower())
            {
                case "development":
                case "dev":
                    server.environment = Server.enumEnvironment.development;
                    break;
                case "staging":
                case "stage":
                    server.environment = Server.enumEnvironment.staging;
                    break;
                case "production":
                case "prod":
                    server.environment = Server.enumEnvironment.production;
                    break;
            }

            //configure server security
            server.bcrypt_workfactor = int.Parse(config.GetSection("Encryption:bcrypt_work_factor").Value);
            server.salt = config.GetSection("Encryption:salt").Value;

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

            //configure cookie-based authentication
            var expires = !string.IsNullOrEmpty(config.GetSection("Session:Expires").Value) ? int.Parse(config.GetSection("Session:Expires").Value) : 60;

            services.AddAuthentication().AddCookie(opts =>
            {
                opts.Cookie.Expiration = TimeSpan.FromMinutes(expires);
                opts.Cookie.Name = server.nameSpace;
            });


            //configure session
            services.AddSession(opts =>
            {
                opts.Cookie.Name = server.nameSpace;
                opts.IdleTimeout = TimeSpan.FromMinutes(expires);
            });
        }

        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //use cookie authentication
            app.UseAuthentication();
            //use session
            app.UseSession();

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
            var errOptions = new DeveloperExceptionPageOptions();
            errOptions.SourceCodeLineCount = 10;
            app.UseDeveloperExceptionPage();

            //server if finished configuring
            Configured(app, env, config);

            //run Datasilk application
            app.Run(async (context) =>
            {
                Run(context);
            });
        }

        public virtual void Configured(IApplicationBuilder app, IHostingEnvironment env, IConfigurationRoot config){}

        public virtual async void Run(HttpContext context)
        {
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;
            var requestStart = DateTime.Now;
            DateTime requestEnd;
            TimeSpan tspan;
            var requestType = "";
            var path = cleanPath(context.Request.Path.ToString());
            var paths = path.Split('/').ToArray();
            var extension = "";

            //get request file extension (if exists)
            if (path.IndexOf(".") >= 0)
            {
                for (int x = path.Length - 1; x >= 0; x += -1)
                {
                    if (path.Substring(x, 1) == ".")
                    {
                        extension = path.Substring(x + 1); return;
                    }
                }
            }

            server.requestCount += 1;

            if (server.environment == Server.enumEnvironment.development)
            {
                Console.WriteLine("--------------------------------------------");
                Console.WriteLine("{0} GET {1}", DateTime.Now.ToString("hh:mm:ss"), path);

                //optionally, wipe Scaffold cache to enable developer updates to html files when server is running
                server.Scaffold = new Dictionary<string, SerializedScaffold>();
            }

            //get form files (if any exist)
            IFormCollection form = null;
            if (context.Request.ContentType != null)
            {
                if (context.Request.ContentType.IndexOf("multipart/form-data") >= 0)
                {
                    form = context.Request.Form;
                }
            }

            byte[] bytes = new byte[0];
            string data = "";
            int dataType = 0; //0 = ajax, 1 = HTML form post, 2 = multi-part form (with file uploads)

            //figure out what kind of data was sent with the request
            if (form == null && context.Request.Body.CanRead)
            {
                //get POST data from request
                using (MemoryStream ms = new MemoryStream())
                {
                    context.Request.Body.CopyTo(ms);
                    bytes = ms.ToArray();
                }
                data = Encoding.UTF8.GetString(bytes, 0, bytes.Length).Trim();
            }
            else
            {
                //form files exist
                dataType = 2;
            }

            if (paths.Length > 1)
            {
                if (paths[0] == "api")
                {
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //run a web service via ajax (e.g. /api/namespace/class/function) //////////////////////////////////////////////////////////////////////////////////////////////////////
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    
                    if (cleanNamespace(paths))
                    {
                        //execute web service
                        requestType = "service";
                        //get parameters from request body, including page id
                        var parms = new Dictionary<string, string>();
                        object[] paramVals;
                        var param = "";
                        

                        if (data.Length > 0)
                        {
                            if (data.IndexOf("Content-Disposition") > 0)
                            {
                                //multi-part file upload
                                dataType = 2;
                            }
                            else if (data.IndexOf("{") >= 0 && data.IndexOf("}") > 0 && data.IndexOf(":") > 0)
                            {
                                //get method parameters from POST S.ajax.post()
                                Dictionary<string, object> attr = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
                                foreach (KeyValuePair<string, object> item in attr)
                                {
                                    parms.Add(item.Key.ToLower(), item.Value.ToString());
                                }
                            }
                            else if (data.IndexOf("=") >= 0)
                            {
                                //HTML form POST data
                                dataType = 1;
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

                        //start building Web API response (find method to execute & return results)
                        var S = new Core(server, context);

                        //load service class from URL path
                        string className = S.Server.nameSpace + ".Services." + paths[1];
                        string methodName = paths[2];
                        if (paths.Length == 4) { className += "." + paths[2]; methodName = paths[3]; }
                        var routes = new global::Routes(S);
                        var service = routes.FromServiceRoutes(className);
                        if (service == null)
                        {
                            try
                            {
                                Type stype = Type.GetType(className);
                                service = (Service)Activator.CreateInstance(stype, new object[] { S });
                            }
                            catch (Exception) { }
                        }

                        //check if service class was found
                        if (service == null)
                        {
                            S.Response.ContentType = "text/html";
                            context.Response.WriteAsync("no service found");
                            S.Unload();
                            return;
                        }

                        if (dataType == 1)
                        {
                            //parse HTML form POST data and send to new Service instance
                            string[] items = S.Server.UrlDecode(data).Split('&');
                            string[] item;
                            for (var x = 0; x < items.Length; x++)
                            {
                                item = items[x].Split('=');
                                service.Form.Add(item[0], item[1]);
                            }
                        }
                        else if (dataType == 2)
                        {
                            //send multi-part file upload data to new Service instance
                            service.Files = form.Files;
                        }

                        //execute method from new Service instance
                        Type type = Type.GetType(className);
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
                                var i = 0;
                                if (int.TryParse(param, out i) == true)
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
                                else if(methodParams[x].ParameterType.IsArray)
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
                                else if(methodParams[x].ParameterType.Name.IndexOf("Dictionary") == 0)
                                {
                                    paramVals[x] = (Dictionary<string, string>)S.Util.Serializer.ReadObject(param, typeof(Dictionary<string, string>));
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
                            if (server.environment == Server.enumEnvironment.development)
                            {
                                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                            }
                            throw ex;
                        }


                        //finally, unload the Datasilk Core:
                        //close SQL connection, save User info, etc (before sending response)
                        S.Unload();
                        context.Response.ContentType = "text/json";
                        if (result != null)
                        {
                            context.Response.WriteAsync((string)result);
                        }
                        else
                        {
                            context.Response.WriteAsync("{\"error\":\"no content returned\"}");
                        }
                    }
                }
            }

            if (requestType == "" && extension == "")
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //page request (initialize client-side application) ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                requestType = "page";
                var S = new Core(server, context);

                //create instance of Page class based on request URL path
                var html = "";
                var newpaths = path.Split('?', 2)[0].Split('/');
                var routes = new global::Routes(S);
                var page = routes.FromPageRoutes(newpaths[0].ToLower());

                if (page == null)
                {
                    //page is not part of any known routes, try getting page class manually
                    Type type = Type.GetType((S.Server.nameSpace + ".Pages." + (newpaths[0] == "" ? "Login" : S.Util.Str.Capitalize(newpaths[0].Replace("-", " ")).Replace(" ", ""))));
                    page = (Page)Activator.CreateInstance(type, new object[] { S });
                }

                if (page != null)
                {
                    //render page
                    if(form != null)
                    {
                        page.Files = form.Files;
                    }
                    try
                    {
                        html = page.Render(newpaths);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                        throw ex;
                    }
                }
                else
                {
                    //show 404 error
                    page = new Page(S);
                    html = page.Error404();
                }

                //unload Datasilk Core
                page = null;
                S.Unload();

                //send response back to client
                S.Response.ContentType = "text/html";
                await S.Response.WriteAsync(html);
            }

            if (server.environment == Server.enumEnvironment.development)
            {
                requestEnd = DateTime.Now;
                tspan = requestEnd - requestStart;
                server.requestTime += (tspan.Seconds);
                Console.WriteLine("END REQUEST {0} ms, {1} {2}", tspan.Milliseconds, path, requestType);
                Console.WriteLine("");
            }
        }

        private string cleanPath(string path)
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

        private bool cleanNamespace(string[] paths)
        {
            //check for malicious namespace in web service request
            foreach(var p in paths)
            {
                if (!p.All(a => char.IsLetter(a))) { return false; }
            }
            return true;
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
    }
}
