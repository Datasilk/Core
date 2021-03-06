![Datasilk Logo](https://www.markentingh.com/projects/datasilk/logo.png)

# Datasilk Core MVC
#### An MVC Framework for ASP.NET Core
Datasilk Core is an ultra-fast, light-weight alternative to ASP.NET Core MVC, it supports Views using HTML with mustache variables, hierarchical Controller rendering, and RESTful web services.

## Startup.cs

Make sure to include the middleware within `Startup.cs`.

``` csharp
app.UseDatasilkMvc(new MvcOptions()
{
	IgnoreRequestBodySize = true,
	WriteDebugInfoToConsole = true,
	Routes = new Routes()
});
```

## Page Requests

All page request URLs are mapped to controllers that inherit the `Datasilk.Core.Web.IController` interface. For example, the URL `http://localhost:7770/products` would map to the class `MyProject.Controllers.Products`.

**/Views/Home/home.html**
``` html
<div class="hero">
	<h1>{{title}}</h1>
	<h3>{{description}}</h3>
</div>
```

**/Controllers/Home.cs**
``` csharp
namespace MyProject.Controllers
{
    public class Home: Datasilk.Core.Web.Controller
    {
        public override string Render(string body = "")
		{
			//render page
			var view = new View("/Views/Home/home.html");
			view["title"] = "Welcome";
			view["description"] = "I like to write software";
			AddScript("/js/views/home/home.js");
			return view.Render();		
		}
	}
}
```

## Web Services
The Datasilk Core MVC framework comes with the ability to call *RESTful* web APIs. All web API calls are executed from `Datasilk.Core.Web.IService` interfaces.

#### Example

``` csharp
namespace MyProject.Services
{
    public class User: Datasilk.Core.Web.Service
    {
		[POST]
		public string Authenticate(string email, string password)
		{
			//authenticate user
			if(Authenticated(email, password))
			{
				return Success();
			}
			else
			{
				return AccessDenied("Incorrect email and/or password");
			}
		}
	}
}
```


Read more [Documentation](https://www.github.com/datasilk/core) on Github