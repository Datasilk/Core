using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Utility.Serialization;
using Utility.Strings;


namespace Datasilk
{

    public partial class User
    {
        public int userId = 0;
        public short userType = 0;
        public string visitorId = "";
        public string email = "";
        public string name = "";
        public string displayName = "";
        public bool photo = false;
        public bool isBot = false;
        public bool useAjax = true;
        public bool isMobile = false;
        public bool isTablet = false;
        public bool resetPass = false;
        public DateTime datecreated;
        public Dictionary<string, string> Data = new Dictionary<string, string>();

        protected bool changed = false;
        protected HttpContext context;

        //constructor
        public User(HttpContext context) { this.context = context; }

        //get User object from session
        public static User Get(HttpContext context)
        {
            User user;
            var keys = context.Session.Keys.ToList();
            if (context.Session.Get("user") != null)
            {
                user = (User)Serializer.ReadObject(context.Session.Get("user").GetString(), typeof(User));
            }
            else
            {
                user = new User(context);
            }
            user.Init(context);
            return user;
        }

        public virtual void Init(HttpContext context)
        {
            //generate visitor id
            this.context = context;
            if (visitorId == "" || visitorId == null) {
                visitorId = Generate.NewId();
                changed = true;
            }
            VendorInit();
        }

        public void Save(bool changed = false)
        { 
            if(this.changed == true && changed == false)
            {
                VendorSave();
                context.Session.Set("user", Serializer.WriteObject(this));
                this.changed = false;
            }
            if(changed == true)
            {
                this.changed = true;
            }
        }
        
        public void LogIn(int userId, string email, string name, DateTime datecreated, string displayName = "", short userType = 1, bool photo = false)
        {
            this.userId = userId;
            this.userType = userType;
            this.email = email;
            this.photo = photo;
            this.name = name;
            this.displayName = displayName;
            this.datecreated = datecreated;

            //allow vendor to load settings for user as well
            VendorLogIn();

            changed = true;
        }

        public void LogOut()
        {
            userId = 0;
            email = "";
            name = "";
            photo = false;
            changed = true;
            VendorLogOut();
        }

        #region "Vendor-specific partial methods"
        partial void VendorInit();
        partial void VendorSave();
        partial void VendorLogIn();
        partial void VendorLogOut();
        #endregion
    }
}
