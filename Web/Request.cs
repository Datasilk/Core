using Microsoft.AspNetCore.Http;

namespace Datasilk.Core.Web
{
    public class Request: IRequest
    {
        public HttpContext Context { get; set; }
        public string Path { get; set; }
        public string[] PathParts { get; set; }
        public Parameters Parameters { get; set; }

        public virtual void Init(HttpContext context, Parameters parameters, string path, string[] pathParts)
        {
            Context = context;
            Parameters = parameters;
            Path = path;
            PathParts = pathParts;
        }

        public virtual void Dispose(){}
    }
}

