using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace Bookmarks.Tests.Api.Infrastructure.Security.Extensions
{
    public class AuthorizationTests
    {
        [Fact]
        public void TestIsAuthorized()
        {
            // arrange
            var claims = new List<Claim> {
                new Claim("DisplayName", "DisplayName"),
                new Claim("UserName", "UserName"),
                new Claim("Email", "Email"),
                new Claim("UserId", "UserId"),
                new Claim("Claims", "a|http://a|role1;role2"),
            };
            var identity = new ClaimsIdentity(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        }
    }
}
