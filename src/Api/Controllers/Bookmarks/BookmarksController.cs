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
using Microsoft.Extensions.Options;
using Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using HtmlAgilityPack;
using Api.Favicon;

namespace Api.Controllers.Bookmarks
{
    [Authorize]
    [Route("/api/v1/bookmarks")]
    public class BookmarksController : ApiBaseController
    {
        readonly ILogger<BookmarksController> _logger;
        readonly IBookmarkRepository _repository;
        readonly FaviconSettings _faviconSettings;
        readonly IWebHostEnvironment _webHostEnv;
        readonly IServiceScopeFactory _servicesFactory;
        readonly IconFetcher _fetcher;

        public BookmarksController(ILogger<BookmarksController> logger, IBookmarkRepository repository,
            IWebHostEnvironment env, IOptions<FaviconSettings> settings,
            IServiceScopeFactory factory, IconFetcher fetcher)
        {
            _logger = logger;
            _repository = repository;
            _webHostEnv = env;
            _faviconSettings = settings?.Value ?? new FaviconSettings();
            _servicesFactory = factory;
            _fetcher = fetcher;
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
                        Id = bookmark.Id,
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
                        var path = EnsureFolderPath(parentPath, existing.DisplayName);

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
                        ChildCount = childCount,
                        Favicon = bookmark.Favicon
                    });

                    if (existing.Type == Store.ItemType.Folder && existingDisplayName != bookmark.DisplayName)
                    {
                        // if we have a folder and change the displayname this also affects ALL sub-elements
                        // therefore all paths of sub-elements where this folder-path is present, need to be updated
                        var newPath = EnsureFolderPath(bookmark.Path, bookmark.DisplayName);
                        var oldPath = EnsureFolderPath(existingPath, existingDisplayName);

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

                    _logger.LogInformation($"Updated Bookmark with ID {item.Id}");

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
                _logger.LogError($"Could not update bookmark entry: {EX.Message}\nstack: {EX.StackTrace}");
                return ProblemDetailsResult(
                    detail: $"Could not update bookmark because of error: {EX.Message}",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: Errors.UpdateBookmarksError,
                    instance: HttpContext.Request.Path);
            }
        }

        /// <summary>
        /// update the sort-order for given ids
        /// </summary>
        /// <param name="sorting"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("sortorder")]
        [ProducesResponseType(typeof(Result<string>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateSortOrder([FromBody] BookmarksSortOrderModel sorting)
        {
            _logger.LogDebug($"Will try to update existing bookmark entry: {sorting}");

            if (sorting.Ids.Count() == 0 || sorting.SortOrder.Count() == 0)
            {
                return InvalidArguments($"Invalid request data supplied. Missing Ids and SortOrders!");
            }
            if (sorting.Ids.Count() != sorting.SortOrder.Count())
            {
                return InvalidArguments($"Invalid request data supplied. Ids and SortOrders need to match in count!");
            }

            try
            {
                var user = this.User.Get();
                var outcome = await _repository.InUnitOfWorkAsync<ActionResult>(async () => {
                    int updateCount = 0;
                    for(int i=0; i<sorting.Ids.Count; i++)
                    {
                        var id = sorting.Ids[i];
                        var item = await _repository.GetBookmarkById(id, user.Username);
                        if (item != null)
                        {
                            _logger.LogInformation($"Will update sortOrder of item '{item.DisplayName}' with value of '{sorting.SortOrder[i]}'");
                            item.SortOrder = sorting.SortOrder[i];
                            await _repository.Update(item);
                            updateCount++;
                        }
                        else
                        {
                            _logger.LogWarning($"Could not find item for id: '{id}'");
                            return (false, ProblemDetailsResult(
                                detail: $"Could not find bookmar by id: {id}",
                                statusCode: StatusCodes.Status500InternalServerError,
                                title: Errors.UpdateBookmarksError,
                                instance: HttpContext.Request.Path
                            ));
                        }
                    }

                    var result = new OkObjectResult(new Result<string> {
                        Success = true,
                        Message = $"Updated '{updateCount}' bookmark items.",
                        Value = updateCount.ToString()
                    });

                    return (true, result);
                });
                return outcome.value;
            }
            catch(Exception EX)
            {
                _logger.LogError($"Could not update bookmarks sort-order: {EX.Message}\nstack: {EX.StackTrace}");
                return ProblemDetailsResult(
                    detail: $"Could not update bookmark sort-order because of error: {EX.Message}",
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

        /// <summary>
        /// use the bookmark id to redirect to the given URL
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("fetch/{id}")]
        [ProducesResponseType(StatusCodes.Status307TemporaryRedirect)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchAndForward(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return InvalidArguments( $"Invalid id supplid");
            }

            _logger.LogDebug($"Try to fetch bookmark by id '{id}'");
            string url = "";
            try
            {
                var outcome = await _repository.InUnitOfWorkAsync<ActionResult>(async () => {

                    var user = this.User.Get();
                    var bookmark = await _repository.GetBookmarkById(id, user.Username);
                    if (bookmark == null || string.IsNullOrEmpty(bookmark.Id))
                    {
                        _logger.LogWarning($"could not get bookmark by id '{id}'");
                        return (false, ProblemDetailsResult(
                            statusCode: StatusCodes.Status404NotFound,
                            title: Errors.NotFoundError,
                            detail: $"No bookmark with given id '{id}' found.",
                            instance: HttpContext.Request.Path
                        ));
                    }

                    if (bookmark.Type == Store.ItemType.Folder)
                    {
                        _logger.LogWarning($"AccessCount and Redirect only valid for Nodes '{id}'");
                        return (false, ProblemDetailsResult(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: Errors.InvalidRequestError,
                            detail: $"Cannot use folder for redirect '{id}'",
                            instance: HttpContext.Request.Path
                        ));
                    }

                    bookmark.AccessCount += 1;
                    var updated = await _repository.Update(bookmark);

                    _logger.LogInformation($"Updated Bookmark.AccessCount for ID {id}");
                    url = bookmark.Url;
                    var result = Redirect(url);

                    if (string.IsNullOrEmpty(bookmark.Favicon))
                    {
                        // fire&forget, run this in background and do not wait for the result
                        _ = FetchFavicon(bookmark,  url);
                    }
                   return (true, result);
                });

                return outcome.value;
            }
            catch(Exception EX)
            {
                _logger.LogError($"Could not forward bookmark url: {EX.Message}\nstack: {EX.StackTrace}");
                return ProblemDetailsResult(
                    detail: $"Could not foreward bookmark url because of error: {EX.Message}",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: Errors.UpdateBookmarksError,
                    instance: HttpContext.Request.Path);
            }
        }

        async Task FetchFavicon(BookmarkEntity bookmark, string url)
        {
            try
            {
                if (_fetcher == null || _servicesFactory == null)
                    return;

                var scope = _servicesFactory.CreateScope();
                var repository = scope.ServiceProvider.GetService(typeof(IBookmarkRepository)) as IBookmarkRepository;
                if (repository == null)
                {
                    _logger.LogWarning("Unable to get a repository from ServicesScopeFactory!");
                    return;
                }

                var result = await _fetcher.GetFaviconFromUrl(url);
                var filename = result.filename;
                var payload = result.payload;
                // combine icon with bookmark id
                filename = bookmark.Id + "_" + filename;
                if (payload != null && payload.Length > 0)
                {
                    _logger.LogDebug($"got a favicon payload of length '{payload.Length}' for url '{url}'");

                    var rootPath = _webHostEnv.ContentRootPath;
                    var iconPath = _faviconSettings.StoragePath;
                    var storagePath = Path.Combine(rootPath, iconPath);
                    var path = Path.Combine(storagePath, filename);
                    await System.IO.File.WriteAllBytesAsync(path, payload);
                    if (System.IO.File.Exists(path))
                    {
                        // also update the favicon
                        await repository!.InUnitOfWorkAsync<bool>(async () => {
                            var bm = bookmark;
                            bm.Favicon = filename;
                            var updated = await repository.Update(bm);
                            return updated != null ? (true, true) : (false, false);
                        });
                    }
                }
                else
                {
                    _logger.LogDebug($"No favicon payload for url '{url}'.");
                }
            }
            catch(Exception EX)
            {
                _logger.LogError($"Error during favicon fetch/update: {EX.Message}; stack: {EX.StackTrace}");
            }
        }

        /// <summary>
        /// get the favicon of a bookmark URL
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("favicon/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult> GetFavicon(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return InvalidArguments( $"Invalid id supplid");
            }

            _logger.LogDebug($"Try to fetch bookmark by id '{id}'");

            var rootPath = _webHostEnv.ContentRootPath;
            var iconPath = _faviconSettings.StoragePath;

            var user = this.User.Get();
            var bookmark = await _repository.GetBookmarkById(id, user.Username);

            if (bookmark != null && !string.IsNullOrEmpty(bookmark.Favicon))
            {
                iconPath = Path.Combine(iconPath, bookmark.Favicon);
                if (!System.IO.File.Exists(iconPath))
                {
                    iconPath = _faviconSettings.DefaultFavicon; // default icon
                }
            }
            else
            {
                iconPath = _faviconSettings.DefaultFavicon; // default icon
            }
            var path = Path.Combine(rootPath, iconPath);
            if (!System.IO.File.Exists(path))
            {
                _logger.LogError($"Path of favicon does not exists: '{path}'");
                return ProblemDetailsResult(
                    detail: $"Could not get favicon!",
                    statusCode: StatusCodes.Status404NotFound,
                    title: Errors.NotFoundError,
                    instance: HttpContext.Request.Path);
            }

            return PhysicalFile(path, "image/x-icon");
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
                Url = entity.Url,
                Favicon = entity.Favicon
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

        // EnsureFolderPath takes care that the supplied path is valid
        // e.g. it does not start with two slashes '//' and that the resulting
        // path is valid, with all necessary delimitors
        string EnsureFolderPath(string path, string displayName)
        {
            var folderPath = path;
            if (!path.EndsWith("/"))
            {
                folderPath = path + "/";
            }
            if (folderPath.StartsWith("//"))
            {
                folderPath = folderPath.Replace("//", "/");
            }
            folderPath += displayName;

            return folderPath;
        }
    }
}
