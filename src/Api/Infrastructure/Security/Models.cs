namespace Api.Infrastructure.Security
{
    public class User
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string UserId { get; set; }
        public Claim[] Claims { get; set; }
    }

    public class Claim
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string[] Roles { get; set; }
    }

     public class JwtSettings
    {
        public string Issuer { get; set; }
        public string Secret { get; set; }
        public string CookieName { get; set; }
        public string LoginRedirect { get; set; }
        public Claim Claims { get; set; }
    }
}
