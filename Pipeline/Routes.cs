namespace Datasilk
{
    public class Routes
    {
        protected Core S;

        public Routes(Core DatasilkCore)
        {
            S = DatasilkCore;
        }

        public virtual Page FromPageRoutes(string name)
        {
            return null;
        }

        public virtual Service FromServiceRoutes(string name)
        {
            return null;
        }
    }
}
