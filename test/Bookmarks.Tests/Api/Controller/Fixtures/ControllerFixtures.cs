using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookmarks.Tests.Api.Controller.Fixtures
{
    public class ControllerFixtures
    {
        public ControllerContext Context
        {
            get
            {
                var claims = new List<Claim> {
                    new Claim("DisplayName", "DisplayName"),
                    new Claim("UserName", "UserName"),
                    new Claim("Email", "Email"),
                    new Claim("UserId", "UserId"),
                    new Claim("Claims", "a|http://a|role1;role2"),
                };
                var identity = new ClaimsIdentity(claims);
                ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                var context = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = principal
                    }
                };
                return context;
            }
        }
    }
}
