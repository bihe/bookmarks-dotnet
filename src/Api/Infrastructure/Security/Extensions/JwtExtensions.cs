using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Builder;
using Api.Infrastructure.Security.Exceptions;
using Api.Infrastructure.Security.Middleware;

namespace Api.Infrastructure.Security.Extensions
{
    public static class JwtExtensions
    {
        public static void AddJwtAuth(this IServiceCollection services, JwtSettings settings)
        {
            services.AddAuthentication(x => {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x => {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                var jwtSharedKey = System.Text.Encoding.UTF8.GetBytes(settings.Secret);
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtSharedKey),
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = false
                };
                x.Events = new JwtBearerEvents
                {
                    OnChallenge = c =>
                    {
                        // if this is a browser request the auth-redirect process should be started
                        if (ContentNegotiation.IsAcceptable(c.Request, "text/html"))
                        {
                            return Task.FromException(new LoginChallengeException("Browser login challenge start"));
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = c =>
                    {
                        if (c.Principal.Identity.IsAuthenticated)
                        {
                            // after authentication is performed ensure that the user
                            // has the necessary rights to access the resources
                            var user = c.Principal.Get();
                            var principal = Authorization.IsAuthorized(c.Principal, settings.Claims, settings.Issuer);
                            if (principal == null)
                            {
                                return Task.FromException(new AuthorizationException("Browser login challenge start"));
                            }
                            c.Principal = principal;
                            return Task.FromResult(true);
                        }
                        return Task.FromResult(false);
                    },
                    OnMessageReceived = c =>
                    {
                        if (string.IsNullOrEmpty(c.Token))
                        {
                            // the token was not received from the "expected location"
                            // fetch the token from the cookie instead!
                            var jwtCookiePayload = c.Request.Cookies[settings.CookieName];
                            if (!string.IsNullOrEmpty(jwtCookiePayload))
                            {
                                c.Token = jwtCookiePayload;
                            }
                        }

                        if (string.IsNullOrEmpty(c.Token))
                            return Task.FromResult(false);

                        return Task.FromResult(true);
                    }
                };
            });
        }

        public static void UseJwtAuth(this IApplicationBuilder app)
        {
            app.UseLoginRedirectHandling();
            app.UseAuthentication();
            app.UseAuthorization();
        }
    }
}
