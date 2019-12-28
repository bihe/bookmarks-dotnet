using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Bookmarks.Tests.Api.Integration
{
    /// <summary>
    /// https://adamstorr.azurewebsites.net/blog/integration-testing-with-aspnetcore-3-1-remove-the-boiler-plate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CustomWebApplicationFactory<T> : WebApplicationFactory<T> where T: class
    {
        public Action<IServiceCollection> Registrations { get; set; }

        public CustomWebApplicationFactory() : this(null)
        {}

        public CustomWebApplicationFactory(Action<IServiceCollection> registrations = null)
        {
            Registrations = registrations ?? (collection => { });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {   builder
                .UseEnvironment("Testing")
                .ConfigureTestServices(services =>
                {
                    // Don't run IHostedServices when running as a test
                    services.RemoveAll(typeof(IHostedService));
                    Registrations?.Invoke(services);
                });
        }
    }
}
