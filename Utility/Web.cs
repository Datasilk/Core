using System.Text;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Utility.Web
{
    public static class HeaderParameters
    {
        public static Parameters GetParameters(HttpContext context)
        {
            var parms = new Parameters();
            var param = "";
            string data = "";
            if (context.Request.ContentType != null && context.Request.ContentType.IndexOf("multipart/form-data") < 0 && context.Request.Body.CanRead)
            {
                //get POST data from request
                byte[] bytes = new byte[0];
                using (MemoryStream ms = new MemoryStream())
                {
                    context.Request.Body.CopyTo(ms);
                    bytes = ms.ToArray();
                }
                data = Encoding.UTF8.GetString(bytes, 0, bytes.Length).Trim();
            }

            if (data.Length > 0)
            {
                parms.Add("_request-body", data);
                if (data.IndexOf("Content-Disposition") < 0 && data.IndexOf("{") >= 0 && data.IndexOf("}") > 0 && data.IndexOf(":") > 0)
                {
                    //get method parameters from POST S.ajax.post()
                    Dictionary<string, object> attr = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
                    foreach (KeyValuePair<string, object> item in attr)
                    {
                        parms.Add(item.Key.ToLower(), item.Value.ToString());
                    }
                }
            }

            //get method parameters from query string
            foreach (var key in context.Request.Query.Keys)
            {
                if (!param.Contains(key))
                {
                    parms.Add(key, context.Request.Query[key].ToString());
                }
            }
            return parms;
        }
    }
}
