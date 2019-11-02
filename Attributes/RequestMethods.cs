using System;

namespace Datasilk.Core.Web
{
    [AttributeUsage(AttributeTargets.Method)]
    public class GETAttribute : Attribute { }
    public class POSTAttribute : Attribute { }
    public class PUTAttribute : Attribute { }
    public class HEADAttribute : Attribute { }
    public class DELETEAttribute : Attribute { }
}
