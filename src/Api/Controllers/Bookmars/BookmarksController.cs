using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Controllers.Bookmarks
{
    [Authorize]
    [ApiController]
    [Produces("application/json")]
    [Route("/api/v1/bookmarks")]
    public class BookmarksController : ControllerBase
    {
        readonly ILogger<BookmarksController> _logger;

        public BookmarksController(ILogger<BookmarksController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            _logger.LogDebug("get bookmarks!");
            return "bookmarks";
        }
    }
}
