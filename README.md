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
      * update the `namespace` value to reflect your web application's namespace
      * update the `data/SqlServerTrusted` value to connect to your SQL Server database.

3. copy `/Core/layout.html` into the root of your ASP.NET Core web application. You can make edits to this file if you need to add custom HTML within the `<head></head>` tag or the foot of your website layout.

4. Open your `/Startup.cs` class file and replace everything with: `public class Startup: Datasilk.Startup{ }`

5. Open your Project Properties, select the `Application` tab, and change `startup object` to use `Program`

6. Build your Project to see if everything compiles correctly with no errors.

That's it! Next, learn how to use the Datasilk Core MVC framework to build web pages & web services.
