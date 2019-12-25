using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Api.Infrastructure.Security;
using Api.Infrastructure.Security.Extensions;
using Store;
using Microsoft.EntityFrameworkCore;
using Api.Infrastructure.Persistence;

namespace Api.Infrastructure
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var jwtSection = Configuration.GetSection("Jwt");
            services.Configure<JwtSettings>(jwtSection);
            var jwtSettings = jwtSection.Get<JwtSettings>();

            // add repository
            services.AddDbContextPool<BookmarkContext>(options => {
                options.UseMySql(Configuration.GetConnectionString("BookmarksConnection"));
            });

            services.AddJwtAuth(jwtSettings);
            services.AddControllers();

            services.AddScoped<IBookmarkRepository, DbBookmarkRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, BookmarkContext context)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseErrorHandling();
            app.UseJwtAuth();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            if (env.IsDevelopment())
            {
                context.Database.EnsureCreated();
            }
        }
    }
}
