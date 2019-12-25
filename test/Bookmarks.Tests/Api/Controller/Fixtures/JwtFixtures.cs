using System.Net.Http.Headers;

namespace Bookmarks.Tests.Api.Controller.Fixtures
{
    public class JwtFixtures
    {
        public string Token => "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE4Nzc1MzIyMjEsImp0aSI6IjAxYzNiZTllLWVmZTItNGViMy04ZjUyLTQxMWRmZDI0NDFjNyIsImlhdCI6MTU3NjkyNzQyMSwiaXNzIjoibG9naW4uYmluZ2dsLm5ldCIsInN1YiI6ImEuYkBjLmRlIiwiVHlwZSI6ImxvZ2luLlVzZXIiLCJEaXNwbGF5TmFtZSI6IkRpc3BsYXlOYW1lIiwiRW1haWwiOiJhLmJAYy5kZSIsIlVzZXJJZCI6IlVzZXJJZCIsIlVzZXJOYW1lIjoiVXNlck5hbWUiLCJHaXZlbk5hbWUiOiJVc2VyIiwiU3VybmFtZSI6Ik5hbWUiLCJDbGFpbXMiOlsiYm9va21hcmtzfGh0dHA6Ly9sb2NhbGhvc3Q6MzAwMHxBZG1pbjtVc2VyIl19.phhEJYyFIpNioH-68ypphKYS3gC373U1duHNhcupH2w";

        public AuthenticationHeaderValue AuthHeader => AuthenticationHeaderValue.Parse($"Bearer {Token}");

        public JwtFixtures()
        {}
    }
}
