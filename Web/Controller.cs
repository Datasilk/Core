using System;
using Microsoft.AspNetCore.Http;

namespace Datasilk.Core.Web
{
    public interface IController: IRequest
    {
        string Render(string body = "");
        string Redirect(string url);
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


    public class Controller: Request, IController
    {
        public virtual string Render(string body = "")
        {
            return body;
        }

        public string Error<T>() where T : IController
        {
            Context.Response.StatusCode = 500;
            return IController.Error<T>(this);
        }

        public string Error404<T>() where T : IController
        {
            Context.Response.StatusCode = 404;
            return IController.Error404<T>(this);
        }

        public string AccessDenied<T>() where T : IController
        {
            Context.Response.StatusCode = 403;
            return IController.AccessDenied<T>(this);
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
