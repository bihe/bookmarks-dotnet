using System;
using System.Collections.Generic;
using System.Security.Claims;
using sec = System.Security.Claims;

namespace Api.Infrastructure.Security.Extensions
{
    public static class Authorization
    {
        public static ClaimsPrincipal? IsAuthorized(ClaimsPrincipal principal, Claim required, string issuer)
        {
            var user = principal.Get();

            var availabelClaims = new List<Claim>(user.Claims);
            var matchingClaim = availabelClaims.Find(x => {
                var found = false;
                if (x.Name == required.Name
                    && CompareUrls(x.Url, required.Url)
                    && FindMatchingRole(x.Roles, required.Roles))
                {
                    found = true;
                }
                return found;
            });
            if (matchingClaim == null)
                return null;

            var claims = new List<sec.Claim> {
                new sec.Claim(ClaimTypes.Name, user.Username, ClaimValueTypes.String, issuer),
                new sec.Claim(ClaimTypes.Email, user.Email, ClaimValueTypes.String, issuer),
                new sec.Claim("DisplayName", user.DisplayName, ClaimValueTypes.String, issuer),
                new sec.Claim("UserName", user.Username, ClaimValueTypes.String, issuer),
                new sec.Claim("Email", user.Email, ClaimValueTypes.String, issuer),
                new sec.Claim("UserId", user.UserId, ClaimValueTypes.String, issuer),
                new sec.Claim("Claims", ClaimToString(matchingClaim), ClaimValueTypes.String, issuer),
            };

            var userIdentity = new ClaimsIdentity(claims, "JWT");
            var userPrincipal = new ClaimsPrincipal(userIdentity);
            return userPrincipal;
        }

        static string ClaimToString(Claim claim)
        {
            return $"{claim.Name}|{claim.Url}|{string.Join(";", claim.Roles)}";
        }

        static bool CompareUrls(string a, string b)
        {
            var uriA = new Uri(a);
            var uriB = new Uri(b);

            // base uri comparison
            if (uriA.Scheme != uriB.Scheme ||
                uriA.Port != uriB.Port ||
                uriA.Host != uriB.Host)
            {
                return false;
            }

            // compare paths
            if (NormalizePath(uriA.PathAndQuery) != NormalizePath(uriB.PathAndQuery))
                return false;

            return true;
        }

        static string NormalizePath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var last = path.Substring(path.Length-1);
                if (last == "/")
                    return path.Substring(0, path.Length-1);
            }
            return path;
        }

        static bool FindMatchingRole(string[] a, string[] b)
        {
            foreach (var r in a)
            {
                foreach (var s in b)
                {
                    if (s == r)
                        return true;
                }
            }
            return false;
        }
    }
}
