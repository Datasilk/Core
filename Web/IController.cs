using System;
using System.Text;

namespace Datasilk.Core.Web
{
    public interface IController: IRequest
    {
        StringBuilder Scripts { get; set; }
        StringBuilder Css { get; set; }

        string Render(string body = "");
        string Redirect(string url);
        void AddScript(string url, string id = "", string callback = "");
        void AddCSS(string url, string id = "");
        string Error<T>() where T : IController;
        string Error(string message = "Error 500");
        string Error404<T>() where T : IController;
        string Error404(string message = "Error 404");
        string AccessDenied<T>() where T : IController;

        static T LoadController<T>(IController parent) where T : IController
        {
            var controller = (T)Activator.CreateInstance(typeof(T));
            controller.Context = parent.Context;
            controller.PathParts = parent.PathParts;
            controller.Path = parent.Path;
            controller.Parameters = parent.Parameters;
            return controller;

        }
        static string AccessDenied<T>(IController parent) where T : IController
        {
            var controller = LoadController<T>(parent);
            return controller.Render();
        }

        static string Error<T>(IController parent) where T : IController
        {
            var controller = LoadController<T>(parent);
            return controller.Render();
        }

        static string Error404<T>(IController parent) where T : IController
        {
            var controller = LoadController<T>(parent);
            return controller.Render();
        }
    }
}
