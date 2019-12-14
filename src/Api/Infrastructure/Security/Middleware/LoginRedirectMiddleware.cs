using System.Threading.Tasks;
using Api.Infrastructure.Security.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Infrastructure.Security.Middleware
{
    public class LoginRedirectMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger _logger;
        private readonly JwtSettings _settings;

        public LoginRedirectMiddleware(RequestDelegate next, ILogger<LoginRedirectMiddleware> logger, IOptions<JwtSettings> settings)
        {
            this.next = next;
            this._logger = logger;
            _settings = settings?.Value ?? new JwtSettings();
        }

        public async Task Invoke(HttpContext context /* other scoped dependencies */)
        {
            try
            {
                await next(context);
            }
            catch (LoginChallengeException ex)
            {
                _logger.LogError($"Invalid authentication. Req [Method: {context.Request.Method}, Host: {context.Request.Host}, Path: {context.Request.Path}]");
                await RedirectToLogin(context, ex);
            }
            catch (AuthorizationException ex)
            {
                _logger.LogError($"Invalid authorization. Req [Method: {context.Request.Method}, Host: {context.Request.Host}, Path: {context.Request.Path}]");
                await RedirectToMissingAuth(context, ex);
            }
        }

        private Task RedirectToLogin(HttpContext context, LoginChallengeException exception)
        {
            _logger.LogInformation($"Perform a redirect to the login-system {_settings.LoginRedirect}");
            context.Response.Redirect(_settings.LoginRedirect);
            return Task.FromResult(0);
        }

        private Task RedirectToMissingAuth(HttpContext context, AuthorizationException exception)
        {
            _logger.LogInformation($"Missing authorization - redirect to auth page {_settings.LoginRedirect}");
            context.Response.Redirect(_settings.LoginRedirect);
            return Task.FromResult(0);
        }
    }

    public static class LoginHandlerMiddlewareExtension
    {
        internal static IApplicationBuilder UseLoginRedirectHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoginRedirectMiddleware>();
        }
    }

}
