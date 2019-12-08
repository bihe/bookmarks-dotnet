using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Api.Infrastructure.Security.Extensions;

namespace Api.Controllers.Systeminfo
{
    public class VersionInfo
    {
        public string Version { get; set; }
        public string BuildNumber { get; set; }
    }

    public class UserInfo
    {
        public string DisplayName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string[] Roles { get; set; }
    }

    public class AppInfo
    {
        public UserInfo UserInfo { get; set; }
        public VersionInfo VersionInfo { get; set; }
    }

    [Authorize]
    [ApiController]
    [Produces("application/json")]
    public class AppInfoController : ControllerBase
    {
        readonly ILogger<AppInfoController> _logger;

        public AppInfoController(ILogger<AppInfoController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("/api/v1/appinfo")]
        public AppInfo Get()
        {
            var user = this.User.Get();
            return new AppInfo{
                UserInfo = new UserInfo
                {
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    UserId = user.UserId,
                    UserName = user.Username,
                    Roles = user.Claims[0].Roles,
                },
                VersionInfo = new VersionInfo
                {
                    Version = SystemVersion.GetAssemblyVersion(),
                    BuildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "BUILD"
                }
            };
        }
    }
}
