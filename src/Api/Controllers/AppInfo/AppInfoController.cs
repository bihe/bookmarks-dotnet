using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Api.Infrastructure.Security.Extensions;
using Microsoft.AspNetCore.Http;

namespace Api.Controllers.Systeminfo
{
    public class VersionInfo
    {
        public string Version { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
    }

    public class UserInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string[] Roles { get; set; } = new string[]{};
    }

    public class AppInfo
    {
        public AppInfo()
        {
            UserInfo = new UserInfo();
            VersionInfo = new VersionInfo();
        }

        public UserInfo UserInfo { get; set; }
        public VersionInfo VersionInfo { get; set; }
    }

    [Authorize]
    public class AppInfoController : ApiBaseController
    {
        readonly ILogger<AppInfoController> _logger;

        public AppInfoController(ILogger<AppInfoController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("/api/v1/appinfo")]
        [ProducesResponseType(typeof(AppInfo),StatusCodes.Status200OK)]
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
