using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Datasilk.Core.Web
{
    public class Service : Request, IService
    {
        public virtual void Init() { }

        public string JsonResponse(dynamic obj)
        {
            Context.Response.ContentType = "text/json";
            return JsonSerializer.Serialize(obj);
        }

        public string AccessDenied(string message = "Error 403")
        {
            Context.Response.StatusCode = 403;
            Context.Response.WriteAsync(message);
            return message;
        }

        public string Error(string message = "Error 500")
        {
            Context.Response.StatusCode = 500;
            Context.Response.WriteAsync(message);
            return message;
        }

        public string BadRequest(string message = "Bad Request 400")
        {
            Context.Response.StatusCode = 400;
            return "Bad Request";
        }

        public string Success()
        {
            return "success"; 
        }

        public string Empty()
        {
            Context.Response.ContentType = "text/json";
            return "{}"; 
        }
    }
}
