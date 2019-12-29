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
    [Route("/api/v1/bookmarks")]
    public class BookmarksController : ApiBaseController
    {
        readonly ILogger<BookmarksController> _logger;
        readonly IBookmarkRepository _repository;

        public BookmarksController(ILogger<BookmarksController> logger, IBookmarkRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        /// <summary>
        /// get a bookmark by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(BookmarkModel),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return ProblemDetailsResult(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: Errors.InvalidRequestError,
                    detail: $"Invalid id supplid",
                    instance: HttpContext.Request.Path
                );
            }

            _logger.LogDebug($"Try to fetch bookmark by id '{id}'");

            var user = this.User.Get();

            var bookmark = await _repository.GetBookmarkById(id, user.Username);
            if (bookmark == null || string.IsNullOrEmpty(bookmark.Id))
            {
                _logger.LogWarning($"could not get bookmark by id '{id}'");
                return ProblemDetailsResult(
                    statusCode: StatusCodes.Status404NotFound,
                    title: Errors.NotFoundError,
                    detail: $"No bookmark with given id '{id}' found.",
                    instance: HttpContext.Request.Path
                );
            }

            return Ok(new BookmarkModel {
                ChildCount = bookmark.ChildCount,
                Created = bookmark.Created,
                DisplayName = bookmark.DisplayName,
                Id = bookmark.Id,
                Modified = bookmark.Modified,
                Path = bookmark.Path,
                SortOrder = bookmark.SortOrder,
                Type = bookmark.Type == Store.ItemType.Folder ? ItemType.Folder : ItemType.Node,
                Url = bookmark.Url
            });
        }

        /// <summary>
        /// Create a bookmark entry, either a bookmark node or a folder
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(Result<string>),StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Create([FromBody] BookmarkModel bookmark)
        {
            _logger.LogDebug($"Will try to create a new bookmark entry: {bookmark}");
            string uri = "/api/v1/bookmarks/{0}";

            if (string.IsNullOrEmpty(bookmark.Path) || string.IsNullOrEmpty(bookmark.DisplayName))
            {
                return ProblemDetailsResult(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: Errors.InvalidRequestError,
                    detail: $"Invalid request data supplied. Missing Path or DisplayName!",
                    instance: HttpContext.Request.Path
                );
            }

            try
            {
                var user = this.User.Get();
                var outcome = await _repository.InUnitOfWorkAsync(async () => {
                    // even if an id is supplied it is deliberately ignored
                    var entity = await _repository.Create(new BookmarkEntity{
                        DisplayName = bookmark.DisplayName,
                        Path = bookmark.Path,
                        SortOrder = bookmark.SortOrder,
                        Type = bookmark.Type == ItemType.Node ? Store.ItemType.Node : Store.ItemType.Folder,
                        Url = bookmark.Url,
                        UserName = user.Username,
                    });
                    return (true, entity);
                });

                _logger.LogInformation($"Bookmark created with ID {outcome.value.Id}");
                return Created(string.Format(uri, outcome.value.Id), new Result<string> {
                    Success = true,
                    Message = $"Bookmark created with ID {outcome.value.Id}",
                    Value = outcome.value.Id
                });
            }
            catch(Exception EX)
            {
                _logger.LogError($"Could not create a new bookmark entry: {EX.Message}\nstack: {EX.StackTrace}");
                return ProblemDetailsResult(
                    detail: $"Could not create bookmark because of error: {EX.Message}",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: Errors.CreateBookmarksError,
                    instance: HttpContext.Request.Path);
            }
        }

        /// <summary>
        /// Update an existring bookmark entry
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [ProducesResponseType(typeof(Result<string>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Update([FromBody] BookmarkModel bookmark)
        {
            _logger.LogDebug($"Will try to update existing bookmark entry: {bookmark}");
            string id = "";

            if (string.IsNullOrEmpty(bookmark.Path)
                || string.IsNullOrEmpty(bookmark.DisplayName)
                || string.IsNullOrEmpty(bookmark.Id)
                )
            {
                return ProblemDetailsResult(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: Errors.InvalidRequestError,
                    detail: $"Invalid request data supplied. Missing ID, Path or DisplayName!",
                    instance: HttpContext.Request.Path
                );
            }

            try
            {
                var user = this.User.Get();
                var outcome = await _repository.InUnitOfWorkAsync<ActionResult>(async () => {
                    var existing = await _repository.GetBookmarkById(bookmark.Id, user.Username);
                    if (existing == null)
                    {
                        _logger.LogWarning($"Could not find a bookmark with the given ID '{bookmark.Id}'");
                        return (true, ProblemDetailsResult(
                            detail: $"No bookmark found by ID: {bookmark.Id}",
                            statusCode: StatusCodes.Status404NotFound,
                            title: Errors.NotFoundError,
                            instance: HttpContext.Request.Path));
                    }

                    var item = await _repository.Update(new BookmarkEntity{
                        Id = bookmark.Id,
                        Created = existing.Created,
                        DisplayName = bookmark.DisplayName,
                        Path = bookmark.Path,
                        SortOrder = bookmark.SortOrder,
                        Type = existing.Type, // it does not make any sense to change the type of a bookmark!
                        Url = bookmark.Url,
                        UserName = user.Username,
                    });

                    _logger.LogInformation($"Updated Bookmark with ID {id}");

                    var result = new OkObjectResult(new Result<string> {
                        Success = true,
                        Message = $"Bookmark with ID '{existing.Id}' was updated.",
                        Value = existing.Id
                    });

                    return (true, result);
                });

                return outcome.value;
            }
            catch(Exception EX)
            {
                _logger.LogError($"Could not update a new bookmark entry: {EX.Message}\nstack: {EX.StackTrace}");
                return ProblemDetailsResult(
                    detail: $"Could not update bookmark because of error: {EX.Message}",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: Errors.UpdateBookmarksError,
                    instance: HttpContext.Request.Path);
            }
        }
    }
}
