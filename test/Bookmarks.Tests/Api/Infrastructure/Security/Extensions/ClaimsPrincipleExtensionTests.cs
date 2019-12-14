using System.Collections.Generic;
using System.Security.Claims;
using Api.Infrastructure.Security.Extensions;
using FluentAssertions;
using Xunit;

namespace Bookmarks.Tests.Api.Infrastructure.Security.Extensions
{
    public class ClaimsPrincipleExtensionTests
    {
        [Fact]
        public void TestClaimsPrinciple_GetUser()
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

            // act
            var user = principal.Get();

            // assert
            user
                .Should()
                .NotBeNull();
            user.DisplayName
                .Should()
                .Be("DisplayName");
            user.Username
                .Should()
                .Be("UserName");
            user.Email
                .Should()
                .Be("Email");
            user.UserId
                .Should()
                .Be("UserId");
            user.Claims
                .Should()
                .Contain(x => x.Name == "a" && x.Url == "http://a");
        }

        [Fact]
        public void TestClaimsPrinciple_GetUser_NoClaims()
        {
            // arrange
            var claims = new List<Claim> {
                new Claim("DisplayName", "DisplayName"),
                new Claim("UserName", "UserName"),
                new Claim("Email", "Email"),
                new Claim("UserId", "UserId"),
                new Claim("Claims", ""),
            };
            var identity = new ClaimsIdentity(claims);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            // act
            var user = principal.Get();

            // assert
            user
                .Should()
                .NotBeNull();
            user.DisplayName
                .Should()
                .Be("DisplayName");
            user.Username
                .Should()
                .Be("UserName");
            user.Email
                .Should()
                .Be("Email");
            user.UserId
                .Should()
                .Be("UserId");
            user.Claims
                .Should()
                .HaveCount(1);
        }
    }
}
