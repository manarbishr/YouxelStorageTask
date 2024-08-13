using StorageMicroservice.Services;
using Serilog;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace StorageMicroservice
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            Log.Logger = new LoggerConfiguration()
           .ReadFrom.Configuration(GetConfiguration())
           .Enrich.FromLogContext()
           .WriteTo.Console()
           .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
           .CreateLogger();

            var storageProvider = builder.Configuration["StorageProvider"];

            if (storageProvider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
            {
                builder.Services.AddSingleton<IStorageService, BlobStorageService>();
            }
            else if (storageProvider.Equals("AWS", StringComparison.OrdinalIgnoreCase))
            {
                builder.Services.AddSingleton<IStorageService, S3StorageService>();
            }
            else
            {
                throw new Exception("Invalid storage provider specified in configuration.");
            }
            builder.Services.AddHostedService<RabbitMqConsumerService>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Storage Microservice API",
                    Description = "An API for managing file storage operations",

                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Storage Microservice API v1");
                c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root (http://localhost:<port>/)
            });

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();

        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog() ;
        private static IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            
        }
    }
}