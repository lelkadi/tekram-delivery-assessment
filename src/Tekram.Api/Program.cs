using Scalar.AspNetCore;
using Serilog;
using Tekram.Api.src.auth.Presentation;
using Tekram.Api.src.orders.Presentation;
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
    // builder.Services.AddTekramOpenApi(); // Swagger disabled for now

    var app = builder.Build();

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TekramDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DbInitializer.SeedAsync(db);
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging();

    // Swagger/Scalar disabled — uncomment when AddTekramOpenApi is fixed
    // if (app.Environment.IsDevelopment())
    // {
    //     app.UseSwagger();
    //     app.UseSwaggerUI();
    //     app.MapScalarApiReference(options =>
    //     {
    //         options.Title = "Tekram API";
    //         options.Theme = ScalarTheme.Purple;
    //         options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    //     });
    // }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapAuthEndpoints();
    app.MapRestaurantEndpoints();
    app.MapOrderEndpoints();

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
