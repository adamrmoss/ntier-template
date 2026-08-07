using System.Text.Json;
using NTierTemplate.Application.Auth;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace NTierTemplate.Api.Auth;

/// <summary>
/// Email/password authentication endpoints.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController(
    ITokenService tokenService,
    IAuthApplicationService authApplicationService,
    IUserApplicationService userApplicationService,
    IOptionsMonitor<JsonSerializerOptions> jsonOptionsMonitor
)
    : ApiControllerBase(jsonOptionsMonitor)
{
    /// <summary>
    /// Register a new account with email and password.
    /// </summary>
    /// <param name="request">Registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message when confirmation email is sent.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authApplicationService.RegisterAndSendConfirmationAsync(
            new RegisterUserRequest
            {
                Email = request.Email,
                Password = request.Password,
                DisplayName = request.DisplayName,
                FirstName = request.FirstName,
                LastName = request.LastName,
            },
            cancellationToken
        );

        if (!result.CreateResult.Succeeded)
        {
            if (result.CreateResult.Errors.Any(error =>
                    error.Contains("already exists", StringComparison.OrdinalIgnoreCase)))
            {
                return this.Conflict(new { message = result.CreateResult.Errors[0] });
            }

            return this.BadRequest(new
            {
                message = "Registration failed.",
                errors = result.CreateResult.Errors,
            });
        }

        if (!result.ConfirmationEmailSent)
        {
            return this.StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Registration succeeded but confirmation email could not be sent." }
            );
        }

        return this.OkJson(new MessageResponse
        {
            Message = "Check your email to confirm your account before signing in.",
        });
    }

    /// <summary>
    /// Sign in with email and password.
    /// </summary>
    /// <param name="request">Login payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token pair on success.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userApplicationService.ValidatePasswordAsync(
            request.Email,
            request.Password,
            cancellationToken
        );

        if (user == null)
        {
            var passwordMatches = await userApplicationService.CheckPasswordAsync(
                request.Email,
                request.Password,
                cancellationToken
            );
            var emailConfirmed = await userApplicationService.IsEmailConfirmedAsync(
                request.Email,
                cancellationToken
            );

            if (passwordMatches && !emailConfirmed)
            {
                return this.Unauthorized(new { message = "Please confirm your email before signing in." });
            }

            return this.Unauthorized(new { message = "Invalid email or password." });
        }

        var tokens = await tokenService.CreateTokenPairAsync(user, cancellationToken);

        return this.OkJson(tokens);
    }

    /// <summary>
    /// Confirm an email address and sign the user in.
    /// </summary>
    /// <param name="request">Confirmation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token pair on success.</returns>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await userApplicationService.ConfirmEmailAsync(
            request.UserId,
            request.Token,
            cancellationToken
        );

        if (!result.Succeeded || result.User == null)
        {
            return this.BadRequest(new
            {
                message = "Email confirmation failed.",
                errors = result.Errors,
            });
        }

        var tokens = await tokenService.CreateTokenPairAsync(result.User, cancellationToken);

        return this.OkJson(tokens);
    }

    /// <summary>
    /// Resend the email confirmation message.
    /// </summary>
    /// <param name="request">Resend payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generic success message.</returns>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation(
        ResendConfirmationRequest request,
        CancellationToken cancellationToken
    )
    {
        await authApplicationService.ResendConfirmationEmailAsync(request.Email, cancellationToken);

        return this.OkJson(new MessageResponse
        {
            Message = "If an unconfirmed account exists for that email, a confirmation message has been sent.",
        });
    }

    /// <summary>
    /// Send a password reset email.
    /// </summary>
    /// <param name="request">Forgot-password payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generic success message.</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken
    )
    {
        await authApplicationService.SendPasswordResetEmailAsync(request.Email, cancellationToken);

        return this.OkJson(new MessageResponse
        {
            Message = "If an account exists for that email, a password reset message has been sent.",
        });
    }

    /// <summary>
    /// Reset a password with a token from email.
    /// </summary>
    /// <param name="request">Reset payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token pair on success.</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await userApplicationService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken
        );

        if (!result.Succeeded || result.User == null)
        {
            return this.BadRequest(new
            {
                message = "Password reset failed.",
                errors = result.Errors,
            });
        }

        var tokens = await tokenService.CreateTokenPairAsync(result.User, cancellationToken);

        return this.OkJson(tokens);
    }

    /// <summary>
    /// Rotate a refresh token and issue a new access token.
    /// </summary>
    /// <param name="request">Refresh payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New token pair on success.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokens = await tokenService.RefreshAsync(request.RefreshToken, cancellationToken);

        if (tokens == null)
        {
            return this.Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        return this.OkJson(tokens);
    }

    /// <summary>
    /// Revoke the caller's refresh token.
    /// </summary>
    /// <param name="request">Optional refresh token payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        }

        return this.NoContent();
    }

    /// <summary>
    /// Return the authenticated user's profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current user profile.</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await userApplicationService.GetCurrentUserAsync(cancellationToken);

        if (user == null)
        {
            return this.Unauthorized();
        }

        return this.OkJson(user);
    }
}
