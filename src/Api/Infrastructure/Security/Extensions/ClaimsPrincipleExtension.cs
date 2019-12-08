using System.Collections.Generic;
using System.Security.Claims;

namespace Api.Infrastructure.Security.Extensions
{
    public static class ClaimsPrincipleExtension
    {
        public static User Get(this ClaimsPrincipal principle)
        {
            var user = new User();
            var claims = new List<Claim>();
            foreach (var claim in principle.Claims)
            {
                switch(claim.Type)
                {
                    case "DisplayName":
                        user.DisplayName = claim.Value;
                        break;
                    case "UserName":
                        user.Username = claim.Value;
                        break;
                    case "Email":
                        user.Email = claim.Value;
                        break;
                     case "UserId":
                        user.UserId = claim.Value;
                        break;
                    case "Claims":
                        var c = Parse(claim.Value);
                        if (!claims.Contains(c))
                            claims.Add(c);
                        break;
                }
            }
            user.Claims = claims.ToArray();
            return user;
        }


        private static Claim Parse(string claim)
        {
            var parts = claim.Split("|", System.StringSplitOptions.RemoveEmptyEntries);
            if (parts != null && parts.Length > 0)
            {
                return new Claim {
                    Name = parts[0],
                    Url = parts[1],
                    Roles = parts[2].Split(";", System.StringSplitOptions.RemoveEmptyEntries)
                };
            }
            return new Claim();
        }

    }

}
