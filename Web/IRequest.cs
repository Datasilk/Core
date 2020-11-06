using System;
using Microsoft.AspNetCore.Http;

namespace Datasilk.Core.Web
{
    public interface IRequest : IDisposable
    {
        HttpContext Context { get; set; }
        string Path { get; set; }
        string[] PathParts { get; set; }
        Parameters Parameters { get; set; }

        void Init(HttpContext context, Parameters parameters, string path, string[] pathParts);
    }
}
