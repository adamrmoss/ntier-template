using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace NTierTemplate.Api;

/// <summary>
/// Base controller providing camelCase JSON serialization helpers for API responses.
/// </summary>
public abstract class ApiControllerBase(IOptionsMonitor<JsonSerializerOptions> jsonOptionsMonitor)
    : ControllerBase
{
    private readonly JsonSerializerOptions camelCaseJsonOptions = jsonOptionsMonitor.Get("CamelCaseOptions");

    /// <summary>
    /// Return an Ok result with the value serialized as camelCase JSON for the frontend.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="value">The object to serialize and return.</param>
    /// <returns>Ok result with JSON body.</returns>
    protected IActionResult OkJson<T>(T value)
    {
        return this.Ok(this.SerializeObject(value));
    }

    /// <summary>
    /// Serialize the value to camelCase JSON using the configured options.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>JSON string in camelCase.</returns>
    protected string SerializeObject<T>(T value)
    {
        return JsonSerializer.Serialize(value, this.camelCaseJsonOptions);
    }
}
