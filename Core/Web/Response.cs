namespace Datasilk.Core.Web
{
    public enum responseType
    {
        replace = 0,
        append = 1,
        before = 2,
        after = 3,
        prepend = 4
    }

    //used to send a JSON object back to the client web browser
    public class Response
    {
        public responseType type { get; set; } = responseType.replace; //type of insert command
        public string selector { get; set; } = ""; //css selector to insert response HTML into
        public string html { get; set; } = ""; //HTML response
        public string javascript { get; set; } = ""; //optional javascript to insert onto the page dynamically
        public string css { get; set; } = ""; //optional CSS to insert onto the page dynamically
        public string json { get; set; } = "";

        public Response(string html = "", string javascript = "", string css = "", string json = "", string selector = "", responseType type = responseType.replace)
        {
            this.html = html;
            this.javascript = javascript;
            this.css = css;
            this.selector = selector;
            this.json = json;
            this.type = type;
        }
    }
}
