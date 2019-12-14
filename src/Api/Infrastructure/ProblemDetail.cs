namespace Api.Infrastructure
{
    public class ProblemDetail
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; }= string.Empty;
        public int Status { get; set; } = 0;
        public string Detail { get; set; }= string.Empty;
    }
}
