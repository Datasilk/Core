![Datasilk Logo](http://www.markentingh.com/projects/datasilk/logo.png)

# Datasilk Core 
#### An MVC Framework for ASP.NET Core
Datasilk Core is an ultra-fast, light-weight alternative to ASP.NET Core MVC 5 that supports HTML scaffolding and simple web services.

Instead of managing a complex ASP.NET Core web application and all of its configuration, simply include this framework within your own ASP.NET Core Web Application project, follow the installation instructions below, and start building your website!

## Installation

1. Include this project within your ASP.NET Core Web Application under a folder named `/Core`
    * Either download the zip file from GitHub...
    * Or use `git submodule add http://github.com/Datasilk/Core`

2. copy `/Core/config.json` into the root of your ASP.NET Core project
	* edit `/config.json` 
      * update the `namespace` value to reflect your web application's namespace. This will allow Datasilk Core to access code from your project correctly
      * update the `data/SqlServerTrusted` value to connect to your SQL Server database.

3. copy `/Core/layout.html` to `/Views/Shared/layout.html`. You can make edits to this file if you need to add custom HTML within the `<head>` tag or the foot of your website layout.

4. copy `/Core/access-denied.html` to `/Views/access-denied.html`.

5. Open your `/Startup.cs` class file and replace everything with: 
	```
	public class Startup: Datasilk.Startup{ }
	```

6. Create a new class `/Routes.cs` and replace everything with:
	```
	using Microsoft.AspNetCore.Http;
	using Datasilk;

	public class Routes : Datasilk.Routes {}
	```
	> NOTE: You can set up your routes once you start creating Page controllers

7. Open your *Project Properties*, select the *Application* tab and change *startup object* to use `Datasilk.Program`

That's it! Next, learn how to use the Datasilk Core MVC framework to build web pages & web services.

## Page Requests

All page request URLs are mapped to controllers that inherit the `Datasilk.Page` class located in the `Pages` namespace for your project (e.g. `MyProject.Pages`). For example, the URL `http://localhost:7770/products` maps to the class `MyProject.Pages.Products`.

> NOTE: Replace "MyProject" with the name of your project in the examples above & below

### Example

**/Views/Home/home.html**
```
<div class="hero">
	<h1>{{title}}</h1>
	<h3>{{description}}</h3>
</div>
```

**/Controllers/Home.cs**
```
using Microsoft.AspNetCore.Http;

namespace MyProject.Pages
{
    public class Home: Datasilk.Page
    {
        public Home(HttpContext context) : base(context) {}

        public override string Render(string[] path, string body = "", object metadata = null)
		{
			//render page
			var scaffold = new Scaffold("/Pages/Home/home.html", Server.Scaffold);
			scaffold.Data["title"] = "Welcome";
			scaffold.Data["description"] = "I like to write software";
			AddScript("/js/views/home/home.js");
			return base.Render(path, scaffold.Render(), metadata);		
		}
	}
}
```

In the example above, a user tries to access the URL `http://localhost:7770/`, which (by default) will render the contents of the `MyProject.Pages.Home` class. This class loads `/Views/Home/home.html` into a `Scaffold` object and replaces the `{{title}}` variable located within the `home.html` file with the text "Welcome!". Then, the page returns `base.Render`, which will render HTML from `Views/Shared/layout.html` along with the contents of `scaffold.Render()`, injected into the `<body>` tag of `Views/Shared/layout.html`. 

> NOTE: `MyProject.Pages.Home` is the default class that is instantiated if the URL contains a domain name with no path structure. 

### Page Hierarchy
To render web pages based on complex URL paths, the Datasilk Core framework relies heavily on the first part of the request path to determine which class to instantiate. For example, if the user accesses the URL `http://localhost:7770/blog/2018/01/21/Progress-Report`, Datasilk Core initializes the `MyProject.Pages.Blog` class. 

The request path is split up into an array and passed into the overridable `Render` function located in your `Datasilk.Page` class. The `paths` array is used to determine what type of content to load for the user. If we're loading a blog post like the above example, we can check the `paths` array to find year, month, and day, followed by the title of the blog post, and determine which blog post to load.

### Datasilk.Page
Inherited in classes that are used to render page requests.
 
### Layout.html
`Views/Shared/layout.html` contains the `<html>`, `<head>` & `<body>` tags for the page, along with `<meta>` tags, `<link/>` tags for CSS, and `<script>` tags or Javascript files.

### Access Denied
If your web page is secure and must display an `Access Denied` page, you can use: 

```return AccessDenied(true, Login(S))```

 from within your `Datasilk.Page` class `Render` method, which will return the contents of the file `Views/access-denied.html`. If a `Datasilk.Page` class is supplied (e.g. `Login(S)`), instead of loading `Views/access-denied.html`, it will render an instance of your `Datasilk.Page` class.

> NOTE: You can find more functionality for the `Page` class inside `/Core/Page.cs`.

## Web Services
The Datasilk Core framework comes with the ability to call `RESTful` web APIs. All web API calls are executed from `Service` classes located in the `Services` namespace within your project (e.g. `MyProject.Services`) and will inherit the `Datasilk.Service` class.

#### Example

```
using Microsoft.AspNetCore.Http;

namespace MyProject.Services
{
    public class User: Datasilk.Service
    {
        public User(HttpContext context) : base(context) {}

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
return Inject(".myclass", injectType.replace, myHtml, myJavascript, myCss)
```

> NOTE: You must first install the optional JavaScript library [Datasilk/CoreJs](https://github.com/Datasilk/CoreJs) and use the JavaScript function `S.ajax` in order to correctly process the JSON response from a Web Service method that returns a `Datasilk.Service.Response` object

## Routes.cs
Your project now includes `Routes.cs`, an empty class file in the root folder. Use it by mapping request path names to new instances of `Datasilk.Page` classes. For example:
```
using Microsoft.AspNetCore.Http;
using Datasilk;

public class Routes : Datasilk.Routes
{
    public override Page FromPageRoutes(HttpContext context, string name)
    {
        switch (name)
        {
            case "": case "home": return new MyProject.Pages.Home(S);
            case "login": return new MyProject.Pages.Login(S);
            case "dashboard": return new MyProject.Pages.Dashboard(S);
        }
        return null;
    }

    public override Service FromServiceRoutes(HttpContext context, string name)
    {
        return null;
    }
}
```

#### Why Routing?
By routing new class instances using the `new` keyword, you bypass the last resort for Datasilk Core, which is to create an instance of your `Page` class using `Activator.CreateInstance`, taking 10 times the amount of CPU ticks to instatiate. You don't have to use routing, but it does speed up performance.


## Optional: Datasilk Core Javascript Library
Learn more about the optional Javascript library, [Datasilk/CoreJs](https://github.com/Datasilk/CoreJs), which contains the appropriate functionality used to make ajax calls and inject content onto the page. The library includes other optional features, such as message alert boxes, popup modals, drag & drop functionality, and HTML templating.