using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        const string ProblemDetailsContentType = "application/problem+json";

        protected ObjectResult ProblemDetailsResult(int statusCode, string title, string detail, string instance)
        {
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = instance
            };
            return new ObjectResult(problemDetails)
            {
                ContentTypes = { ProblemDetailsContentType },
                StatusCode = statusCode,
            };
        }
    }
}
