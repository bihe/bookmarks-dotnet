namespace Api.Controllers.Bookmarks
{
    public class Result<T> where T : class
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public T? Value { get; set; }
    }

    public class ListResult<T> : Result<T> where T : class
    {
        public int Count { get; set; }
    }
}
