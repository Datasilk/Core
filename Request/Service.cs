using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Datasilk
{
    public class Service : Request
    {
        public Dictionary<string, string> Form = new Dictionary<string, string>();
        public IFormFileCollection Files;

        public enum injectType
        {
            replace = 0,
            append = 1,
            before = 2,
            after = 3
        }

        public struct Response
        {
            public injectType type;
            public string selector;
            public string html;
            public string javascript;
            public string css;
        }

        public Service(Core DatasilkCore) : base(DatasilkCore)
        {
            S = DatasilkCore;
        }

        public string AccessDenied()
        {
            return "access denied";
        }

        public string Success()
        {
            return "success";
        }

        public string Inject(string selector, injectType injectType, string html, string javascript, string css)
        {
            var response = new Response()
            {
                type = injectType,
                selector = selector,
                html = html,
                javascript = javascript,
                css = css
            };
            return "{\"d\":" + S.Util.Serializer.WriteObjectToString(response) + "}";
        }

        public string Inject(Response response)
        {
            return Inject(response.selector, response.type, response.html, response.javascript, response.css);
        }
    }
}
