using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Produces(MediaTypeNames.Application.Json)]
    public abstract class ApiBaseController : ControllerBase
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
