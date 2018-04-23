using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Utility.Serialization;
using Utility.Strings;


namespace Datasilk
{

    public class User
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

        public virtual void Init()
        {
            //generate visitor id
            if (visitorId == "" || visitorId == null) {
                visitorId = Generate.NewId();
                changed = true;

                //check for authentication cookie
                var identity = (ClaimsIdentity)context.User.Identity;
                if(identity != null)
                {
                    if(identity.IsAuthenticated == true)
                    {
                        userId = int.Parse(identity.FindFirst(ClaimTypes.NameIdentifier).Value);
                        userType = short.Parse(identity.FindFirst("userType").Value);
                        email = identity.FindFirst(ClaimTypes.Email).Value;
                        photo = identity.FindFirst("photo").Value == "1";
                        name = identity.FindFirst(ClaimTypes.Name).Value;
                        displayName = identity.FindFirst("displayName").Value;
                        datecreated = DateTime.Parse(identity.FindFirst("dateCreated").Value);
                        visitorId = identity.FindFirst("visitorId").Value;
                    }
                }
            }
        }

        public void Save(bool changed = false)
        { 
            if(this.changed == true || changed == true)
            {
                context.Session.Set("user", Serializer.WriteObject(this));
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
            changed = true;

            //create authentication cookie on sign in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("visitorId", visitorId),
                new Claim("displayName", displayName),
                new Claim("userType", userType.ToString()),
                new Claim("photo", photo == true ? "1" : "0"),
                new Claim("dateCreated", datecreated.ToShortDateString() + " " + datecreated.ToLongTimeString())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        public void LogOut()
        {
            userId = 0;
            email = "";
            name = "";
            photo = false;
            changed = true;
            context.Session.Remove("user");

            //update authentication cookie when sign out
            context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
