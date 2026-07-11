using Microsoft.EntityFrameworkCore;
using Serilog;
using Tekram.Api.src.auth.Presentation;
using Tekram.Api.src.orders.Presentation;        // #16 scope — uncomment when merged
using Tekram.Api.src.restaurants.Presentation;
using Tekram.Api.src.shared;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddTekramAuth(builder.Configuration);
    builder.Services.AddTekramServices(builder.Configuration);
    builder.Services.AddTekramRateLimiting();
    builder.Services.AddTekramCors();
    var app = builder.Build();

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        await db.Database.MigrateAsync();
        await DbInitializer.SeedAsync(db);
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging();

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapAuthEndpoints();
    app.MapRestaurantEndpoints();
    app.MapOrderEndpoints();        // #16 scope — uncomment when merged

    app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
