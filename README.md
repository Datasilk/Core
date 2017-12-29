# Datasilk Core 
#### An MVC Framework for ASP.NET Core
A ultra-fast, light-weight alternative to ASP.NET Core MVC 5 that supports HTML scaffolding and simple web services.

Instead of managing a complex ASP.NET Core web application and all of its configuration, simply include this framework within your own ASP.NET Core Web Application project, follow the installation instructions below, and start building your website!

## Installation

1. Include this project within your ASP.NET Core Web Application under a folder named `/Core`
    * Either download the zip file from GitHub...
    * Or use `git submodule add http://github.com/Datasilk/Core`

2. copy `/Core/config.json` into the root of your ASP.NET Core web application
	* edit `/config.json` 
      * update the `namespace` value to reflect your web application's namespace. This will ensure that page requests work by loading the correct `Page` classes from your project namespace.
      * update the `data/SqlServerTrusted` value to connect to your SQL Server database.

3. copy `/Core/layout.html` into the root of your ASP.NET Core web application. You can make edits to this file if you need to add custom HTML within the `<head></head>` tag or the foot of your website layout.

4. copy `/Core/access-denied.html` into the root of your ASP.NET Core web application.

4. Open your `/Startup.cs` class file and replace everything (including the namespace) with: `public class Startup: Datasilk.Startup{ }`

5. Open your Project Properties, select the `Application` tab, and change `startup object` to use `Datasilk.Program`

That's it! Next, learn how to use the Datasilk Core MVC framework to build web pages & web services.

## Page Requests

All page requests are executed from `Page` classes located in the `Pages` namespace for your project (e.g. `MyProject.Pages`) and will inherit the `Datasilk.Page` class.

#### Example

```
namespace MyProject.Pages
{
    public class Home: Datasilk.Page
    {
        public Home(Core MyCore) : base(MyCore) {}

        public override string Render(string[] path, string body = "", object metadata = null)
		{
			//render page
			var scaffold = new Scaffold(S, "/Pages/Home/home.html");
			scaffold.Data["title"] = "Welcome!";
			return base.Render(path, scaffold.Render(), metadata);		
		}
	}
}
```

In the example above, a user tries to access the URL `http://localhost:7770/`, which (by default) will render the contents of the `MyProject.Pages.Home` class. This class loads `/Pages/Home/home.html` into a `Scaffold` object and replaces the `{{title}}` variable located within the `home.html` file with the text "Welcome!". Then, the page returns `base.Render`, which will render HTML from `/layout.html` along with the contents of `scaffold.Render()`, injected into the `<body>` tag of `/layout.html`. 

> NOTE: `MyProject.Pages.Home` is the default class that is instantiated if the URL contains a domain name with no folder structure. 

### Global `S` Object

The `S` object (or "Super" object) is a global object that gives developers access to the `HttpContext` object via `S.Context`, the Web Server class  via `S.Server` (along with server-wide caching functionality), a persistant user object that is unique to each user session via `S.User`, and other `HttpContext` pointer objects via `S.Request`, `S.Response`, `S.Session`.
 
### Layout.html
`/layout.html` contains the `<html></html>`, `<head></head>` & `<body></body>` tags for the page, along with `<meta></meta>` tags, `<link/>` tags for CSS, and `<script></script>` tags or Javascript files.

### Access Denied
If your web page is secure and must display an `Access Denied` page, you can render: 

```return AccessDenied(true, Login(S))```

 from within your `Render` function, which will return the contents of the file `/access-denied.html`. If a `Login` class is supplied, instead of loading `/access-denied.html`, it will render an instance of your `Login` `Datasilk.Page` class.

> NOTE: You can find more functionality for the `Page` class inside `/Core/Request/Page.cs`.

## Web Services
The Datasilk Core framework comes with the ability to call `RESTful` web APIs. All web API calls are executed from `Service` classes located in the `Services` namespace within your project (e.g. `MyProject.Services`) and will inherit the `Datasilk.Service` class.

#### Example

```
namespace MyProject.Services
{
    public class User: Datasilk.Service
    {
        public User(Core MyCore) : base(MyCore) {}

        public string Authenticate(string email, string password)
		{
			//authenticate user
			//...
			if(authenticated){
				return Success();
			}else{
				S.Response.StatusCode = 500;
				return "";
			}
		}
	}
}
```

In the example above, the user would send an `AJAX` `POST` via Javascript to the URL `/api/User/Authenticate` to authenticate their email & password. The data sent to the server would be formatted using `JSON.stringify({email:myemail, password:mypass});`, and the data properties would be mapped to C# method arguments.

### Web Service Response Object
All `Datasilk.Service` methods should return a string, but can also return a `Datasilk.Service.Response` object, which will allow the user to specify where in the DOM to inject HTML, how it should be injected (replace, append, before, after), and whether or not to load some custom javascript code or CSS styles. For example:

```
return Inject(".myclass", Datasilk.Service.injectType.replace, myHtml, myJavascript, myCss)
```

## Optional: Datasilk Core Javascript Library
Learn more about the optional Javascript library, [Datasilk/CoreJs](https://github.com/Datasilk/CoreJs), which contains the appropriate functionality used to make ajax calls and inject content onto the page from the results of a `Datasilk.Service.Response` JSON object.