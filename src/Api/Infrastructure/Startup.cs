using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Api.Infrastructure.Security;
using Api.Infrastructure.Security.Extensions;
using Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.IO;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Api.Favicon;

namespace Api.Infrastructure
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            CurrentEnvironment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment CurrentEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var jwtSection = Configuration.GetSection("Jwt");
            services.Configure<JwtSettings>(jwtSection);
            var jwtSettings = jwtSection.Get<JwtSettings>();

            var faviconSection = Configuration.GetSection("Favicon");
            services.Configure<FaviconSettings>(faviconSection);

            // sloppy workaround to prevent mysql db connection in testing-scenarios
            if (CurrentEnvironment.EnvironmentName != "Testing")
            {
                services.AddDbContextPool<BookmarkContext>(options => {
                    options.UseMySql(Configuration.GetConnectionString("BookmarksConnection"));
                });
            }

            services.AddJwtAuth(jwtSettings);
            services.AddControllers();
            services.AddRazorPages();
            // add repository: scoped because DBContext is also scoped
            services.AddScoped<IBookmarkRepository, DbBookmarkRepository>();

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bookmarks API", Version = "v1" });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddSingleton<IconFetcher>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, BookmarkContext context, IOptions<JwtSettings> jwtSettings)
        {
            app.UseRouting();
            app.UseErrorHandling();
            app.UseJwtAuth();
            app.UseSwagger();
            app.UseStaticFiles();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookmarks API V1");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });

            if (env.IsDevelopment())
            {
                context.Database.EnsureCreated();
            }

            // fallback route for html5 client routing
            // https://weblog.west-wind.com/posts/2017/Aug/07/Handling-HTML5-Client-Route-Fallbacks-in-ASPNET-Core
            app.Run(async (context) =>
            {
                // only do this for html-requesting clients
                if (ContentNegotiation.IsAcceptable(context.Request, "text/html"))
                {
                    var authenticated = context?.User?.Identity?.IsAuthenticated ?? false;
                    if (!authenticated)
                    {
                        context!.Response.Redirect(jwtSettings.Value.LoginRedirect);
                    }
                    else
                    {
                        context!.Response.ContentType = "text/html";
                        await context.Response.SendFileAsync(Path.Combine(env.WebRootPath, "ui/index.html"));
                    }
                }
            });
        }
    }
}
