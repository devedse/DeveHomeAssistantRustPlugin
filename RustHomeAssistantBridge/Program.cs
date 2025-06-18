
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using RustHomeAssistantBridge.Configuration;
using RustHomeAssistantBridge.Data;
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

            // Add Entity Framework
            builder.Services.AddDbContext<RustBridgeDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? "Data Source=rustbridge.db"));

            // Configuration
            builder.Services.Configure<HomeAssistantConfig>(builder.Configuration.GetSection("HomeAssistant"));
            builder.Services.Configure<FcmConfig>(builder.Configuration.GetSection("Fcm"));

            // HTTP Client for Home Assistant
            builder.Services.AddHttpClient<HomeAssistantService>();

            // Register services
            builder.Services.AddSingleton<HomeAssistantService>();
            builder.Services.AddSingleton<MultiServerRustPlusService>();
            builder.Services.AddHostedService<MultiServerRustBridgeHostedService>();
            builder.Services.AddHostedService<FcmListenerService>();

            var app = builder.Build();

            // Ensure database is created
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<RustBridgeDbContext>();
                context.Database.EnsureCreated();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Rust Home Assistant Bridge API V1");
            });

            //Redirect to /openapi/v1.json
            var option = new RewriteOptions();
            option.AddRedirect("^$", "swagger");
            app.UseRewriter(option);

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
