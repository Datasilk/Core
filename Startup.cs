using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Datasilk
{
    public class Startup
    {
        protected Server server;

        public virtual void ConfigureServices(IServiceCollection services)
        {
            //set up server-side memory cache
            services.AddDistributedMemoryCache();
            services.AddMemoryCache();

            services.AddSession(opts =>
            {
                //set up cookie expiration
                opts.Cookie.Name = "Datasilk";
                opts.IdleTimeout = TimeSpan.FromMinutes(60);
            });
        }

        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            //load application-wide memory store
            server = new Server();

            //use session
            app.UseSession();

            //handle static files
            var options = new StaticFileOptions {ContentTypeProvider = new FileExtensionContentTypeProvider()};
            app.UseStaticFiles(options);

            //exception handling
            var errOptions = new DeveloperExceptionPageOptions();
            errOptions.SourceCodeLineCount = 10;
            app.UseDeveloperExceptionPage();

            var config = new ConfigurationBuilder()
                .AddJsonFile(server.MapPath("config.json"))
                .AddEnvironmentVariables().Build();

            server.config = config;

            server.nameSpace = config.GetSection("Namespace").Value;
            server.sqlActive = config.GetSection("Data:Active").Value;
            server.sqlConnectionString = config.GetSection("Data:" + server.sqlActive).Value;
            
            switch (config.GetSection("Environment").Value.ToLower())
            {
                case "development": case "dev":
                    server.environment = Server.enumEnvironment.development;
                    break;
                case "staging": case "stage":
                    server.environment = Server.enumEnvironment.staging;
                    break;
                case "production": case "prod":
                    server.environment = Server.enumEnvironment.production;
                    break;
            }

            //configure server security
            server.bcrypt_workfactor = int.Parse(config.GetSection("Encryption:bcrypt_work_factor").Value);
            server.salt = config.GetSection("Encryption:salt").Value;

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
            }

            if (paths.Length > 1)
            {
                if (paths[0] == "api")
                {
                    //run a web service via ajax (e.g. /api/namespace/class/function)
                    IFormCollection form = null;
                    if (context.Request.ContentType != null)
                    {
                        if (context.Request.ContentType.IndexOf("application/x-www-form-urlencoded") >= 0)
                        {
                        }
                        else if (context.Request.ContentType.IndexOf("multipart/form-data") >= 0)
                        {
                            //get files collection from form data
                            form = await context.Request.ReadFormAsync();
                        }
                    }
                    //form = await context.Request.ReadFormAsync();

                    if (cleanNamespace(paths))
                    {
                        //execute web service
                        var ws = new WebService(server, context, paths, form);
                        requestType = "service";
                    }
                }
            }

            if (requestType == "" && extension == "")
            {
                //initial page request
                var r = new PageRequest(server, context, path);
                requestType = "page";
            }

            if (server.environment == Server.enumEnvironment.development)
            {
                requestEnd = DateTime.Now;
                tspan = requestEnd - requestStart;
                server.requestTime += (tspan.Seconds);
                Console.WriteLine("END GET {0} {1} ms {2}", path, tspan.Milliseconds, requestType);
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
    }
}
