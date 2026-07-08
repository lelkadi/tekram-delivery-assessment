namespace Tekram.Api.src.auth.Infrastructure;

using Tekram.Api.src.auth.Application.Interfaces;

public class LoggingNotificationGateway : INotificationGateway
{
    private readonly ILogger<LoggingNotificationGateway> _logger;
    private readonly bool _emailMock;
    private readonly bool _smsMock;

    public LoggingNotificationGateway(IConfiguration configuration, ILogger<LoggingNotificationGateway> logger)
    {
        _logger = logger;
        _emailMock = configuration.GetValue<bool>("EMAIL_MOCK", true);
        _smsMock = configuration.GetValue<bool>("SMS_MOCK", true);
    }

    public Task SendOtpAsync(string? email, string? phone, string channel, string code,
        CancellationToken ct = default)
    {
        if (channel == "email" && _emailMock)
        {
            _logger.LogInformation("[EMAIL_MOCK] OTP for {Email}: {Code}", email, code);
        }
        else if (channel == "phone" && _smsMock)
        {
            _logger.LogInformation("[SMS_MOCK] OTP for {Phone}: {Code}", phone, code);
        }
        else
        {
            _logger.LogWarning("Real {Channel} gateway not configured. OTP would be sent to {Destination}.",
                channel, channel == "email" ? email : phone);
        }

        return Task.CompletedTask;
    }
}
