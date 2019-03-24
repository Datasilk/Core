using System.Reflection;

namespace Datasilk.Models
{
    public class Config
    {
        public string assembly = "";
        public string environment = "development";
        public string defaultController = "Home";
        public ConfigSections.Sql sql = new ConfigSections.Sql();
        public ConfigSections.Encryption encryption = new ConfigSections.Encryption();

        public Config()
        {
            if(assembly == "")
            {
                assembly = Assembly.GetExecutingAssembly().GetName().Name;
            }
        }
    }
}

namespace Datasilk.Models.ConfigSections
{ 
    public class Sql
    {
        public string active = "SqlServerTrusted";
        public string SqlServerTrusted = "server=.\\SQL2017; database=YOUR_DATABASE_NAME; Trusted_Connection=true";
    }

    public class Encryption
    {
        public string salt = "?";
        public string bcrypt_work_factor = "10";
    }
}
