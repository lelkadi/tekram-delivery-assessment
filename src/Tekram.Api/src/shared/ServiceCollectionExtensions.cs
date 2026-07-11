namespace Tekram.Api.src.shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using FluentValidation;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Tekram.Api.src.auth.Application.Interfaces;
using Tekram.Api.src.auth.Infrastructure;
using Tekram.Api.src.restaurants.Application.Interfaces;
using Tekram.Api.src.restaurants.Infrastructure;
// using Tekram.Api.src.orders.Application.Interfaces;         // #15-#17 scope
// using Tekram.Api.src.orders.Infrastructure;                  // #15-#17 scope

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTekramServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- EF Core / Postgres ----
        services.AddDbContext<TekramDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // ---- Redis ----
        var redisConnectionString = configuration.GetConnectionString("Redis")
                                    ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            try
            {
                return ConnectionMultiplexer.Connect(new ConfigurationOptions
                {
                    EndPoints = { redisConnectionString },
                    AbortOnConnectFail = false
                });
            }
            catch
            {
                return null!;
            }
        });

        // ---- FluentValidation ----
        services.AddValidatorsFromAssemblyContaining<Program>();

        // ---- Auth infrastructure ----
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenProvider, JwtTokenProvider>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddSingleton<INotificationGateway, LoggingNotificationGateway>();

        // ---- Auth handlers ----
        services.AddScoped<auth.Application.Handlers.RegisterUserHandler>();
        services.AddScoped<auth.Application.Handlers.LoginHandler>();
        services.AddScoped<auth.Application.Handlers.VerifyOtpHandler>();
        services.AddScoped<auth.Application.Handlers.ResendOtpHandler>();

        // ---- Restaurants infrastructure ----
        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();

        // ---- Restaurants handlers ----
        services.AddScoped<restaurants.Application.Handlers.SearchRestaurantsHandler>();
        services.AddScoped<restaurants.Application.Handlers.GetMenuHandler>();

        // ---- Orders infrastructure (uncomment when #15-#17 land) ----
        // services.AddScoped<IOrderRepository, OrderRepository>();
        // services.AddScoped<ICouponRepository, CouponRepository>();
        // services.AddScoped<IMenuPricingReader, MenuPricingReader>();

        // ---- Orders handlers (uncomment when #16 lands) ----
        // services.AddScoped<orders.Application.Handlers.PlaceOrderHandler>();

        return services;
    }

    public static IServiceCollection AddTekramRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("otp_resend", config =>
            {
                config.PermitLimit = 3;
                config.Window = TimeSpan.FromMinutes(15);
                config.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;
            });
        });

        return services;
    }

    public static IServiceCollection AddTekramCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }

    public static IServiceCollection AddTekramOpenApi(this IServiceCollection services)
    {
        services.AddSwaggerGen();

        return services;
    }
}
