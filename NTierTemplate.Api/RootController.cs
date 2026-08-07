using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NTierTemplate.Api;

/// <summary>
/// Root health endpoint for the NTierTemplate API.
/// </summary>
[ApiController]
[Route("/")]
[AllowAnonymous]
public class RootController : ControllerBase
{
    /// <summary>
    /// Return a simple health check payload.
    /// </summary>
    /// <returns>API status.</returns>
    [HttpGet]
    public IActionResult Get()
    {
        return this.Ok(new { status = "ok", service = "NTierTemplate.Api" });
    }
}
