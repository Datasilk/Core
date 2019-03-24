namespace Datasilk.Web
{
    public enum responseType
    {
        replace = 0,
        append = 1,
        before = 2,
        after = 3
    }

    public class Response
    {
        public responseType type = responseType.replace; //type of insert command
        public string selector = ""; //css selector to insert response HTML into
        public string html = ""; //HTML response
        public string javascript = ""; //optional javascript to insert onto the page dynamically
        public string css = ""; //optional CSS to insert onto the page dynamically
    }
}
