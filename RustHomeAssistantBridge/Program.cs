
using RustHomeAssistantBridge.Configuration;
using RustHomeAssistantBridge.Services;

namespace RustHomeAssistantBridge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Configuration
            builder.Services.Configure<RustPlusConfig>(builder.Configuration.GetSection("RustPlus"));
            builder.Services.Configure<HomeAssistantConfig>(builder.Configuration.GetSection("HomeAssistant"));

            // HTTP Client for Home Assistant
            builder.Services.AddHttpClient<HomeAssistantService>();

            // Register services
            builder.Services.AddSingleton<HomeAssistantService>();
            builder.Services.AddSingleton<RustPlusService>();
            builder.Services.AddHostedService<RustBridgeHostedService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
