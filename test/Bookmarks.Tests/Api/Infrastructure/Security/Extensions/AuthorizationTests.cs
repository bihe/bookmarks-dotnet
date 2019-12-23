using System.Collections.Generic;
using System.Security.Claims;
using Api.Infrastructure.Security.Extensions;
using api = Api.Infrastructure.Security;
using Xunit;
using FluentAssertions;

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
            var claim = new api.Claim{
                Name = "a",
                Url = "http://a",
                Roles = new string[]{"role1"},
            };

            // act
            var authPrinicpal = Authorization.IsAuthorized(principal, claim, "issuer");

            // assert
            authPrinicpal
                .Should()
                .NotBeNull();
            authPrinicpal.Identity.AuthenticationType
                .Should()
                .Be("JWT");
            authPrinicpal.Claims
                .Should()
                .HaveCount(7);
            authPrinicpal.Claims
                .Should()
                .Contain(x => x.Type == "DisplayName" && x.Value == "DisplayName");
            authPrinicpal.Claims
                .Should()
                .Contain(x => x.Type == "UserName" && x.Value == "UserName");
        }

        [Fact]
        public void TestIsAuthorized_PathAndTrailingSlash()
        {
            // arrange
            var claims = new List<Claim> {
                new Claim("DisplayName", "DisplayName"),
                new Claim("UserName", "UserName"),
                new Claim("Email", "Email"),
                new Claim("UserId", "UserId"),
                new Claim("Claims", "a|http://a/path/|role1;role2"),
            };
            var identity = new ClaimsIdentity(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            var claim = new api.Claim{
                Name = "a",
                Url = "http://a/path",
                Roles = new string[]{"role1"},
            };

            // act
            var authPrinicpal = Authorization.IsAuthorized(principal, claim, "issuer");

            // assert
            authPrinicpal
                .Should()
                .NotBeNull();
            authPrinicpal.Identity.AuthenticationType
                .Should()
                .Be("JWT");
            authPrinicpal.Claims
                .Should()
                .HaveCount(7);
            authPrinicpal.Claims
                .Should()
                .Contain(x => x.Type == "DisplayName" && x.Value == "DisplayName");
            authPrinicpal.Claims
                .Should()
                .Contain(x => x.Type == "UserName" && x.Value == "UserName");

        }

        [Fact]
        public void TestIsAuthorized_NoMatch()
        {
            // arrange
            var claims = new List<Claim> {
                new Claim("DisplayName", "DisplayName"),
                new Claim("UserName", "UserName"),
                new Claim("Email", "Email"),
                new Claim("UserId", "UserId"),
                new Claim("Claims", "b|http://b|role1;role2"),
            };
            var identity = new ClaimsIdentity(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            var claim = new api.Claim{
                Name = "a",
                Url = "http://a",
                Roles = new string[]{"role1"},
            };

            // act
            var authPrinicpal = Authorization.IsAuthorized(principal, claim, "issuer");

            // assert
            authPrinicpal
                .Should()
                .BeNull();

        }

        [Fact]
        public void TestIsAuthorized_NoMatchRole()
        {
            // arrange
            var claims = new List<Claim> {
                new Claim("DisplayName", "DisplayName"),
                new Claim("UserName", "UserName"),
                new Claim("Email", "Email"),
                new Claim("UserId", "UserId"),
                new Claim("Claims", "a|http://a|role3"),
            };
            var identity = new ClaimsIdentity(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            var claim = new api.Claim{
                Name = "a",
                Url = "http://a",
                Roles = new string[]{"role1"},
            };

            // act
            var authPrinicpal = Authorization.IsAuthorized(principal, claim, "issuer");

            // assert
            authPrinicpal
                .Should()
                .BeNull();
        }

        [Fact]
        public void TestIsAuthorized_NoMatchURLs()
        {
            // arrange
            var claims = new List<Claim> {
                new Claim("DisplayName", "DisplayName"),
                new Claim("UserName", "UserName"),
                new Claim("Email", "Email"),
                new Claim("UserId", "UserId"),
                new Claim("Claims", "a|http://www.a.com|role1"),
            };
            var identity = new ClaimsIdentity(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            var claim = new api.Claim{
                Name = "a",
                Url = "http://a",
                Roles = new string[]{"role1"},
            };

            // act
            var authPrinicpal = Authorization.IsAuthorized(principal, claim, "issuer");

            // assert
            authPrinicpal
                .Should()
                .BeNull();
        }

        [Fact]
        public void TestIsAuthorized_NoMatchPath()
        {
            // arrange
            var claims = new List<Claim> {
                new Claim("DisplayName", "DisplayName"),
                new Claim("UserName", "UserName"),
                new Claim("Email", "Email"),
                new Claim("UserId", "UserId"),
                new Claim("Claims", "a|http://a/path1|role1"),
            };
            var identity = new ClaimsIdentity(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            var claim = new api.Claim{
                Name = "a",
                Url = "http://a/path2/",
                Roles = new string[]{"role1"},
            };

            // act
            var authPrinicpal = Authorization.IsAuthorized(principal, claim, "issuer");

            // assert
            authPrinicpal
                .Should()
                .BeNull();
        }
    }
}
