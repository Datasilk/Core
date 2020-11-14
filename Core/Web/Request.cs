using Microsoft.AspNetCore.Http;

namespace Datasilk.Core.Web
{
    public abstract class Request: IRequest
    {
        public HttpContext Context { get; set; }
        public string Path { get; set; }
        public string[] PathParts { get; set; }
        public Parameters Parameters { get; set; }
        public virtual void Dispose() { }
    }
}

