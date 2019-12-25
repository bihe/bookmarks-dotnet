using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Bookmarks.Tests.Api.Controller.Fixtures
{
    public class JsonFixtures
    {
        const string ContentType = "application/json";

        public JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonFixtures()
        {}

        public StringContent Payload<T>(T payload)
        {
            var jsonString = JsonSerializer.Serialize(payload);
            return new StringContent(jsonString, Encoding.UTF8, ContentType);
        }
    }
}
