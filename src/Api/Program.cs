using System;
using Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        static string Env = string.Empty;

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            Env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            Console.Out.WriteLine($"Start application in mode {Env}.");

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Configure the app here.
                    config.AddJsonFile("_etc/appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"_etc/appsettings.{Env}.json", optional: true)
                        .AddEnvironmentVariables();

                    var configuration = config.Build();
                    Log.Logger = new LoggerConfiguration()
                       .ReadFrom.Configuration(configuration)
                       .CreateLogger();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseSerilog();
                })
                ;
        }

    }
}
