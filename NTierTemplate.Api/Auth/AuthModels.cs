namespace NTierTemplate.Api.Auth;

/// <summary>
/// Login request payload.
/// </summary>
public class LoginRequest
{
    public required string Email { get; set; }

    public required string Password { get; set; }
}

/// <summary>
/// Registration request payload.
/// </summary>
public class RegisterRequest
{
    public required string Email { get; set; }

    public required string Password { get; set; }

    public string? DisplayName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}

/// <summary>
/// Refresh request payload.
/// </summary>
public class RefreshRequest
{
    public required string RefreshToken { get; set; }
}

/// <summary>
/// Logout request payload.
/// </summary>
public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

/// <summary>
/// Email confirmation request payload.
/// </summary>
public class ConfirmEmailRequest
{
    public required int UserId { get; set; }

    public required string Token { get; set; }
}

/// <summary>
/// Resend email confirmation request payload.
/// </summary>
public class ResendConfirmationRequest
{
    public required string Email { get; set; }
}

/// <summary>
/// Forgot password request payload.
/// </summary>
public class ForgotPasswordRequest
{
    public required string Email { get; set; }
}

/// <summary>
/// Reset password request payload.
/// </summary>
public class ResetPasswordRequest
{
    public required string Email { get; set; }

    public required string Token { get; set; }

    public required string NewPassword { get; set; }
}

/// <summary>
/// Simple message response for anonymous auth actions.
/// </summary>
public class MessageResponse
{
    public required string Message { get; set; }
}

/// <summary>
/// Token response returned from login, register, and refresh.
/// </summary>
public class TokenResponse
{
    public required string AccessToken { get; set; }

    public required string RefreshToken { get; set; }

    public required DateTime AccessTokenExpiresAt { get; set; }
}
