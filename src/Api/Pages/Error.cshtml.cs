using Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace Api.Pages
{
    public class Error : PageModel
    {
        public string Message { get; private set; } = string.Empty;

        public void OnGet()
        {
            // retrieve a possible error message from the cookie
            var errorString = this.HttpContext.Request.Cookies[Constants.ERROR_COOKIE_NAME];
            if (!string.IsNullOrEmpty(errorString))
            {
                Message = errorString;
                this.HttpContext.Response.Cookies.Delete(Constants.ERROR_COOKIE_NAME);
            }
        }
    }
}
