using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Api.Infrastructure
{
    public class ContentNegotiation
    {
        // found here: https://stackoverflow.com/questions/255436/parse-accept-header
        public static bool IsAcceptable(HttpRequest request, string mediaType) =>
            request.Headers["Accept"].Any(headerValue =>
                !string.IsNullOrWhiteSpace(headerValue) &&
                    headerValue.Split(",").Any(segment => MediaTypeHeaderValue.Parse(segment).MediaType == mediaType));
    }
}
