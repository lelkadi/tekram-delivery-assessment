namespace Tekram.Api.src.shared;

public static class ErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string InternalError = "internal_error";

    // Auth
    public const string EmailAlreadyExists = "email_already_exists";
    public const string PhoneAlreadyExists = "phone_already_exists";
    public const string InvalidCredentials = "invalid_credentials";
    public const string VerificationRequired = "verification_required";

    // OTP
    public const string InvalidOrExpiredCode = "invalid_or_expired_code";
    public const string OtpResendCooldown = "otp_resend_cooldown";

    // Orders
    public const string ItemUnavailable = "item_unavailable";
    public const string InvalidCoupon = "invalid_coupon";
    public const string InvalidCustomization = "invalid_customization";
    public const string InsufficientBalance = "insufficient_balance";

    // Rate limiting
    public const string TooManyRequests = "too_many_requests";
}

public class DomainException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public DomainException(int statusCode, string errorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
