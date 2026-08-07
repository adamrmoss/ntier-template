using System.Text.Json;
using NTierTemplate.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace NTierTemplate.Api.Users;

/// <summary>
/// Exposes account user endpoints.
/// </summary>
[ApiController]
[Route("user")]
[Authorize]
public class UserController(
    IUserApplicationService userApplicationService,
    IOptionsMonitor<JsonSerializerOptions> jsonOptionsMonitor
)
    : ApiControllerBase(jsonOptionsMonitor)
{
    /// <summary>
    /// Return the authenticated user's profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current user as camelCase JSON.</returns>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var user = await userApplicationService.GetCurrentUserAsync(cancellationToken);

        if (user == null)
        {
            return this.Unauthorized();
        }

        return this.OkJson(user);
    }
}
