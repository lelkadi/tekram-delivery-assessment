namespace Tekram.Api.src.shared;

using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using FluentValidation;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

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
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { redisConnectionString },
                AbortOnConnectFail = false
            }));

        // ---- FluentValidation ----
        services.AddValidatorsFromAssemblyContaining<Program>();

        // ---- Auth infrastructure ----
        // services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        // services.AddScoped<ITokenProvider, JwtTokenProvider>();
        // services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<IOtpRepository, OtpRepository>();
        // services.AddSingleton<INotificationGateway, LoggingNotificationGateway>();

        // ---- Auth handlers ----
        // services.AddScoped<auth.Application.Handlers.RegisterUserHandler>();
        // services.AddScoped<auth.Application.Handlers.LoginHandler>();
        // services.AddScoped<auth.Application.Handlers.VerifyOtpHandler>();
        // services.AddScoped<auth.Application.Handlers.ResendOtpHandler>();

        // ---- Restaurants infrastructure ----
        // services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        // services.AddScoped<IMenuRepository, MenuRepository>();

        // ---- Restaurants handlers ----
        // services.AddScoped<restaurants.Application.Handlers.SearchRestaurantsHandler>();
        // services.AddScoped<restaurants.Application.Handlers.GetMenuHandler>();

        // ---- Orders infrastructure ----
        // services.AddScoped<IOrderRepository, OrderRepository>();
        // services.AddScoped<ICouponRepository, CouponRepository>();
        // services.AddScoped<IMenuPricingReader, MenuPricingReader>();

        // ---- Orders handlers ----
        // services.AddScoped<orders.Application.Handlers.PlaceOrderHandler>();

        return services;
    }

    public static IServiceCollection AddTekramRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // ---- Login: 5 attempts per 15 min per IP ----
            options.AddPolicy("login", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    });
            });

            // ---- OTP resend: 3 attempts per 15 min per user ----
            options.AddPolicy("otp_resend", context =>
            {
                var userId = context.User.FindFirstValue("sub") ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(userId, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    });
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
