using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Infrastructure.Security.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Store;
using System.Linq;

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
                return InvalidArguments( $"Invalid id supplid");
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

            return Ok(ToModel(bookmark));
        }

        /// <summary>
        /// get bookmarks by path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("bypath")]
        [ProducesResponseType(typeof(ListResult<List<BookmarkModel>>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetBookmarksByPath([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return InvalidArguments($"Invalid path supplid");
            }

            _logger.LogDebug($"Try to fetch bookmarks for path '{path}'");

            var user = this.User.Get();
            var bookmarks = await _repository.GetBookmarksByPath(path, user.Username);
            if (bookmarks == null)
            {
                bookmarks = new List<BookmarkEntity>();
            }
            return Ok(new ListResult<List<BookmarkModel>>{
                Success = true,
                Value = ToModelList(bookmarks),
                Count = bookmarks.Count,
                Message = $"Found {bookmarks.Count} items."
            });
        }

        [HttpGet]
        [Route("folder")]
        [ProducesResponseType(typeof(Result<BookmarkModel>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetBookmarkFolderByPath([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return InvalidArguments($"Invalid path supplid");
            }

            _logger.LogDebug($"Try to fetch bookmark folder for the given path '{path}'");

            var user = this.User.Get();
            if (path == "/")
            {
                // special treatment for the root path. This path is ALWAYS available
                // and does not have a specific storage entry - this is by convention
                return Ok(new Result<BookmarkModel>{
                    Success = true,
                    Value = new BookmarkModel {
                        DisplayName = "Root",
                        Path = "/",
                        Type = ItemType.Folder,
                        SortOrder = 0,
                        Id = $"{user.Username}_ROOT"
                    },
                    Message = $"Found bookmark folder for path {path}."
                });
            }

            var bookmark = await _repository.GetFolderByPath(path, user.Username);
            if (bookmark == null)
            {
                _logger.LogWarning($"Could not find a bookmark follder by path '{path}'");
                return ProblemDetailsResult(
                    detail: $"No bookmark folder found by path: {path}",
                    statusCode: StatusCodes.Status404NotFound,
                    title: Errors.NotFoundError,
                    instance: HttpContext.Request.Path);
            }
            return Ok(new Result<BookmarkModel>{
                Success = true,
                Value = ToModel(bookmark),
                Message = $"Found bookmark folder for path {path}."
            });
        }


        /// <summary>
        /// get bookmarks by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("byname")]
        [ProducesResponseType(typeof(ListResult<List<BookmarkModel>>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetBookmarksByName([FromQuery] string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return InvalidArguments($"Invalid name supplid");
            }

            _logger.LogDebug($"Try to fetch bookmarks by name '{name}'");

            var user = this.User.Get();
            var bookmarks = await _repository.GetBookmarksByName(name, user.Username);
            if (bookmarks == null)
            {
                bookmarks = new List<BookmarkEntity>();
            }
            return Ok(new ListResult<List<BookmarkModel>>{
                Success = true,
                Value = ToModelList(bookmarks),
                Count = bookmarks.Count,
                Message = $"Found {bookmarks.Count} items."
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
                return InvalidArguments($"Invalid request data supplied. Missing Path or DisplayName!");
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
                return InvalidArguments($"Invalid request data supplied. Missing ID, Path or DisplayName!");
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

                    var childCount = existing.ChildCount;
                    if (existing.Type == Store.ItemType.Folder)
                    {
                        // on save of a folder, update the child-count!
                        var parentPath = existing.Path;
                        if (!parentPath.EndsWith("/"))
                        {
                            parentPath += "/";
                        }
                        var path = parentPath + existing.DisplayName;
                        var nodeCounts = await _repository.GetChildCountOfPath(path, user.Username);
                        if (nodeCounts != null && nodeCounts.Count > 0)
                        {
                            var nodeCount = nodeCounts.Find(x => x.Path == path);
                            if (nodeCount != null)
                            {
                                childCount = nodeCount.Count;
                            }
                        }
                    }
                    var existingDisplayName = existing.DisplayName;
                    var existingPath = existing.Path;

                    var item = await _repository.Update(new BookmarkEntity{
                        Id = bookmark.Id,
                        Created = existing.Created,
                        DisplayName = bookmark.DisplayName,
                        Path = bookmark.Path,
                        SortOrder = bookmark.SortOrder,
                        Type = existing.Type, // it does not make any sense to change the type of a bookmark!
                        Url = bookmark.Url,
                        UserName = user.Username,
                        ChildCount = childCount
                    });

                    if (existing.Type == Store.ItemType.Folder && existingDisplayName != bookmark.DisplayName)
                    {
                        // if we have a folder and change the displayname this also affects ALL sub-elements
                        // therefore all paths of sub-elements where this folder-path is present, need to be updated
                        var newPath = $"{bookmark.Path}/{bookmark.DisplayName}";
                        var oldPath = $"{existingPath}/{existingDisplayName}";

                        _logger.LogDebug($"will update all old paths '{oldPath}' to new path '{newPath}'.");

                        var bookmarks = await _repository.GetBookmarksByPathStart(oldPath, user.Username);
                        if (bookmarks == null)
                            bookmarks = new List<BookmarkEntity>();
                        foreach(var bm in bookmarks)
                        {
                            var updatedPath = bm.Path.Replace(oldPath, newPath);
                            await _repository.Update(new BookmarkEntity{
                                Id = bm.Id,
                                Created = existing.Created,
                                DisplayName = bm.DisplayName,
                                Path = updatedPath,
                                SortOrder = bm.SortOrder,
                                Type = bm.Type,
                                Url = bm.Url,
                                UserName = bm.UserName,
                                ChildCount = bm.ChildCount
                            });
                        }
                    }

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

        /// <summary>
        /// remove an existring bookmark entry
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(typeof(Result<string>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(string id)
        {
            _logger.LogDebug($"Will try to delete existing bookmark with ID '{id}'");

            if (string.IsNullOrEmpty(id))
            {
                return InvalidArguments($"Invalid request data supplied. Missing ID!");
            }

            try
            {
                var user = this.User.Get();
                var outcome = await _repository.InUnitOfWorkAsync<ActionResult>(async () => {
                    var existing = await _repository.GetBookmarkById(id, user.Username);
                    if (existing == null)
                    {
                        _logger.LogWarning($"Could not find a bookmark with the given ID '{id}'");
                        return (true, ProblemDetailsResult(
                            detail: $"No bookmark found by ID: {id}",
                            statusCode: StatusCodes.Status404NotFound,
                            title: Errors.NotFoundError,
                            instance: HttpContext.Request.Path));
                    }

                    // if the element is a folder and there are child-elements
                    // prevent the deletion - this can only be done via a recursive deletion like rm -rf
                    if (existing.Type == Store.ItemType.Folder && existing.ChildCount > 0)
                    {
                        _logger.LogWarning($"Cannot delete folder-elements '{existing.Path}/{existing.DisplayName}' because the item has child-elements '{existing.ChildCount}!");
                        return (false, ProblemDetailsResult(
                            detail: $"Could not delete '{existing.DisplayName}' because of child-elements!",
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: Errors.DeleteBookmarkError,
                            instance: HttpContext.Request.Path));
                    }

                    var ok = await _repository.Delete(existing);
                    if (ok)
                    {
                        _logger.LogInformation($"Updated Bookmark with ID {id}");
                        var result = new OkObjectResult(new Result<string> {
                            Success = true,
                            Message = $"Bookmark with ID '{existing.Id}' was deleted.",
                            Value = existing.Id
                        });
                        return (ok, result);
                    }
                    return (false, ProblemDetailsResult(
                            detail: $"Could not delete bookmark by ID: {id}",
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: Errors.DeleteBookmarkError,
                            instance: HttpContext.Request.Path));
                });

                return outcome.value;
            }
            catch(Exception EX)
            {
                _logger.LogError($"Could not delete the bookmark entry with ID '{id}': {EX.Message}\nstack: {EX.StackTrace}");
                return ProblemDetailsResult(
                    detail: $"Could not delete bookmark because of error: {EX.Message}",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: Errors.DeleteBookmarkError,
                    instance: HttpContext.Request.Path);
            }
        }


        List<BookmarkModel> ToModelList(List<BookmarkEntity> entities)
        {
            return entities.Select(x => ToModel(x)).ToList();
        }

        BookmarkModel ToModel(BookmarkEntity entity)
        {
            return new BookmarkModel {
                ChildCount = entity.ChildCount,
                Created = entity.Created,
                DisplayName = entity.DisplayName,
                Id = entity.Id,
                Modified = entity.Modified,
                Path = entity.Path,
                SortOrder = entity.SortOrder,
                Type = entity.Type == Store.ItemType.Folder ? ItemType.Folder : ItemType.Node,
                Url = entity.Url
            };
        }

        ObjectResult InvalidArguments(string message)
        {
            return ProblemDetailsResult(
                statusCode: StatusCodes.Status400BadRequest,
                title: Errors.InvalidRequestError,
                detail: message,
                instance: HttpContext.Request.Path
            );
        }
    }
}
