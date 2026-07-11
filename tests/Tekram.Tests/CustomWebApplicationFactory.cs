namespace Tekram.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? "Host=localhost;Port=5432;Database=tekram_lane2;Username=postgres;Password=postgres");
        builder.UseSetting("EMAIL_MOCK", "true");
        builder.UseSetting("SMS_MOCK", "true");
    }
}
