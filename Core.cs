using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

public class Core
{
    public Server Server;
    public Utility.Util Util;
    public Datasilk.User User;
    public HttpContext Context;
    public HttpRequest Request;
    public HttpResponse Response;
    public ISession Session;

    public Core(Server server, HttpContext context)
    {
        Server = server;
        Util = server.Util;
        Context = context;
        Request = context.Request;
        Response = context.Response;
        Session = context.Session;
        User = new Datasilk.User();

        //load user session
        if (Session.Get("user") != null)
        {
            User = (Datasilk.User)Util.Serializer.ReadObject(Util.Str.GetString(Session.Get("user")), User.GetType());
        }
        User.Init(this);
    }

    public void Unload()
    {
        if (User.saveSession == true)
        {
            Session.Set("user", Util.Serializer.WriteObject(User));
        }
    }
}

