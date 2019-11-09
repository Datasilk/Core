﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Datasilk.Core.Extensions
{
    public static class Mvc
    {
        public static IApplicationBuilder UseDatasilkMvc(this IApplicationBuilder builder, MvcOptions options = default)
        {
            return builder.UseMiddleware<Middleware.Mvc>(options);
        }
    }

    public class MvcOptions : Middleware.MvcOptions { }
}
