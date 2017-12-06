namespace Datasilk
{
    public class Request
    {
        protected Core S;


        public Request(Core DatasilkCore)
        {
            S = DatasilkCore;
        }

        public bool CheckSecurity()
        {
            if (S.User.userId > 0)
            {
                return true;
            }
            return false;
        }

        public string Error()
        {
            return "error";
        }
    }
}

