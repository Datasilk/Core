namespace Datasilk.Core.Web
{
    public interface IService : IRequest
    {
        void Init();
        string Success();
        string Empty();
        string AccessDenied(string message = "Error 403");
        string Error(string message = "Error 500");
        string BadRequest(string message = "Bad Request 400");
    }
}
