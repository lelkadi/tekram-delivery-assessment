namespace Tekram.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tekram.Api.src.shared;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            "Host=localhost;Port=5432;Database=tekram_test;Username=postgres;Password=postgres");
        builder.UseSetting("EMAIL_MOCK", "true");
        builder.UseSetting("SMS_MOCK", "true");
    }
}
