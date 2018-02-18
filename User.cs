using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;


namespace Datasilk
{

    public class User
    { 

        [JsonIgnore]
        public Core S;

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

        [JsonIgnore]
        public bool saveSession = false;

        public void Init(Core DatasilkCore)
        {
            S = DatasilkCore;

            //generate visitor id
            if (visitorId == "" || visitorId == null) {
                visitorId = S.Util.Str.CreateID(); saveSession = true;

                //check for authentication cookie
                var identity = (ClaimsIdentity)S.Context.User.Identity;
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

        public virtual void Load()
        { 
        }

        public void LogIn(int userId, string email, string name, DateTime datecreated, string displayName = "", short userType = 1, bool photo = false)
        {
            Load();
            this.userId = userId;
            this.userType = userType;
            this.email = email;
            this.photo = photo;
            this.name = name;
            this.displayName = displayName;
            this.datecreated = datecreated;
            saveSession = true;

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

            S.Context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        public void LogOut()
        {
            Load();
            userId = 0;
            email = "";
            name = "";
            photo = false;
            saveSession = true;
            S.Session.Remove("user");

            //update authentication cookie when sign out
            S.Context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
