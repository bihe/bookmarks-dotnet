using System;
using System.Threading.Tasks;
using Api.Infrastructure.Security.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Store;

namespace Api.Controllers.Bookmarks
{
    [Authorize]
    [ApiController]
    [Produces("application/json")]
    [Route("/api/v1/bookmarks")]
    public class BookmarksController : ControllerBase
    {
        readonly ILogger<BookmarksController> _logger;
        readonly IBookmarkRepository _repository;

        public BookmarksController(ILogger<BookmarksController> logger, IBookmarkRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        /// <summary>
        /// Create a bookmark entry, either a bookmark node or a folder
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] BookmarkModel bookmark)
        {
            _logger.LogDebug($"Will try to create a new bookmark entry: {bookmark}");
            string id = "";
            string uri = "";

            if (string.IsNullOrEmpty(bookmark.Path) || string.IsNullOrEmpty(bookmark.DisplayName))
            {
                return Problem(
                    $"Invalid request data supplied. Missing Path or DisplayName!",
                    null,
                    StatusCodes.Status400BadRequest,
                    "InvalidRequestError",
                    "about:blank");
            }

            try
            {
                var user = this.User.Get();

                await _repository.InUnitOfWorkAsync(async () => {
                    var entity = await _repository.Create(new BookmarkEntity{
                        DisplayName = bookmark.DisplayName,
                        Path = bookmark.Path,
                        SortOrder = bookmark.SortOrder,
                        Type = bookmark.Type == ItemType.Node ? Store.ItemType.Node : Store.ItemType.Folder,
                        Url = bookmark.Url,
                        UserName = user.Username,
                    });
                    id = entity.Id;
                    return true;
                });

                _logger.LogInformation($"Bookmark created with ID {id}");
                return Created(uri, new Result<string> {
                    Success = true,
                    Message = $"Bookmark created with ID {id}"
                });
            }
            catch(Exception EX)
            {
                _logger.LogError($"Could not create a new bookmark entry: {EX.Message}\nstack: {EX.StackTrace}");
                return Problem(
                        $"Could not create bookmark because of error: {EX.Message}",
                        null,
                        StatusCodes.Status500InternalServerError,
                        "CreateBookmarksError",
                        "about:blank");
            }
        }
    }
}
