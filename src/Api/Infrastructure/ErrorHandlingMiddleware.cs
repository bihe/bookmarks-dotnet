using System;
using System.Text.Json;
using System.Threading.Tasks;
using Api.Infrastructure.Security.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure
{
    public class ErrorHandlingMiddleware
    {
        readonly RequestDelegate next;
        readonly ILogger _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            this.next = next;
            this._logger = logger;
        }

        public async Task Invoke(HttpContext context /* other scoped dependencies */)
        {
            try
            {
                await next(context);
            }
            catch(AuthorizationException)
            {
                // this exception is handled by it's own middleware
                throw;
            }
            catch(LoginChallengeException)
            {
                // this exception is handled by it's own middleware
                throw;
            }
            catch (Exception EX)
            {
                await ProcessException(context, EX);
            }
        }

        async Task ProcessException(HttpContext context, Exception EX)
        {
            if (ContentNegotiation.IsAcceptable(context.Request, "text/html"))
            {
                // TODO: need of a razor page for errors
                context.Response.Redirect("/Error");
                context.Response.Cookies.Append(Constants.ERROR_COOKIE_NAME, EX.Message);
                context.Response.StatusCode = 302;
            }
            else
            {
                var httpReq = context.Request;
                var req = $"{httpReq.Method}: {httpReq.Scheme}://{httpReq.Host}{httpReq.Path}";
                var errorMessage = EX.Message;
                if (EX.InnerException != null)
                {
                    errorMessage += ", " + EX.InnerException.Message;
                }

                // for every other case, we will return JSON errors
                var error = new ProblemDetails
                {
                    Type = "about:blank",
                    Title = $"error during request: {req}",
                    Status = 500,
                    Detail = errorMessage,
                    Instance = "{httpReq.Scheme}://{httpReq.Host}{httpReq.Path}"
                };
                var result = JsonSerializer.Serialize(error);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(result);
            }
        }
    }

    public static class ErrorHandlingMiddlewareExtension
    {
        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
