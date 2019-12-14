namespace Api.Infrastructure.Security
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Claim[] Claims { get; set; } = new Claim[]{};
    }

    public class Claim
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string[] Roles { get; set; } = new string[]{};
    }

     public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string CookieName { get; set; } = string.Empty;
        public string LoginRedirect { get; set; } = string.Empty;
        public Claim Claims { get; set; } = new Claim();
    }
}
