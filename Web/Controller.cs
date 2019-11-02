using System;

namespace Datasilk.Core.Web
{
    public interface IController: IRequest
    {
        string Render(string body = "");
        string Redirect(string url);
        static T LoadController<T>() where T : IController
        {
            var controller = (T)Activator.CreateInstance(typeof(T));
            return controller;

        }
        static string AccessDenied<T>() where T : IController
        {
            var controller = LoadController<T>();
            return controller.Render();
        }
    }


    public class Controller: Request, IController
    {
        public virtual string Render(string body = "")
        {
            return body;
        }

        public string AccessDenied<T>() where T : IController
        {
            return IController.AccessDenied<T>();
        }

        public string Redirect(string url)
        {
            return "<script language=\"javascript\">window.location.href = '" + url + "';</script>";
        }

        public override void AddScript(string url, string id = "", string callback = "")
        {
            if (ContainsResource(url)) { return; }
            Scripts.Append("<script language=\"javascript\"" + (id != "" ? " id=\"" + id + "\"" : "") + " src=\"" + url + "\"" +
                (callback != "" ? " onload=\"" + callback + "\"" : "") + "></script>");
        }

        public override void AddCSS(string url, string id = "")
        {
            if (ContainsResource(url)) { return; }
            Css.Append("<link rel=\"stylesheet\" type=\"text/css\"" + (id != "" ? " id=\"" + id + "\"" : "") + " href=\"" + url + "\"></link>");
        }

        public bool ContainsResource(string url)
        {
            if (Resources.Contains(url)) { return true; }
            Resources.Add(url);
            return false;
        }
    }
}
