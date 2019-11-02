namespace Datasilk.Core.Middleware
{
    public class MvcOptions
    {
        /// <summary>
        /// If true, removes the request body size limit
        /// </summary>
        public bool IgnoreRequestBodySize { get; set; } = false;

        /// <summary>
        /// If true, outputs details about each request to the console window
        /// </summary>
        public bool WriteDebugInfoToConsole { get; set; } = false;

        /// <summary>
        /// A list of paths that is used to access Web API services. The default is: new string[] { "api" }
        /// </summary>
        public string[] ServicePaths { get; set; } = new string[] { "api" };

        /// <summary>
        /// Reference to a class that can quickly create instances of Controller & Service classes based on request URI routes
        /// </summary>
        public Web.Routes Routes { get; set; } = new Web.Routes();

        /// <summary>
        /// Set the default controller to load if the request URI path is empty
        /// </summary>
        public string DefaultController { get; set; } = "home";

    }
}
