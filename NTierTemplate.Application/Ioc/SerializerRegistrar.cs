using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace NTierTemplate.Application.Ioc;

/// <summary>
/// Registers named JSON serializer options for HTTP, queue, and third-party integrations.
/// </summary>
public static class SerializerRegistrar
{
    /// <summary>
    /// Named options for camelCase JSON consumed by the frontend.
    /// </summary>
    public const string CamelCaseOptionsName = "CamelCaseOptions";

    /// <summary>
    /// Named options for PascalCase JSON used on the server and in queue messages.
    /// </summary>
    public const string PascalCaseOptionsName = "PascalCaseOptions";

    /// <summary>
    /// Named options for snake_case JSON used by third-party integrations.
    /// </summary>
    public const string SnakeCaseOptionsName = "SnakeCaseOptions";

    /// <summary>
    /// Register named <see cref="JsonSerializerOptions"/> for camelCase, PascalCase, and snake_case serialization.
    /// </summary>
    /// <param name="serviceCollection">The service collection to register serializers with.</param>
    public static void RegisterSerializers(this IServiceCollection serviceCollection)
    {
        serviceCollection.Configure<JsonSerializerOptions>(CamelCaseOptionsName, ConfigureCamelCase);
        serviceCollection.Configure<JsonSerializerOptions>(PascalCaseOptionsName, ConfigurePascalCase);
        serviceCollection.Configure<JsonSerializerOptions>(SnakeCaseOptionsName, ConfigureSnakeCase);
    }

    /// <summary>
    /// Apply camelCase JSON settings for frontend HTTP payloads.
    /// </summary>
    /// <param name="options">The serializer options to configure.</param>
    public static void ConfigureCamelCase(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }

    /// <summary>
    /// Apply PascalCase JSON settings for server and queue payloads.
    /// </summary>
    /// <param name="options">The serializer options to configure.</param>
    public static void ConfigurePascalCase(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = null;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }

    /// <summary>
    /// Apply snake_case JSON settings for third-party integrations.
    /// </summary>
    /// <param name="options">The serializer options to configure.</param>
    public static void ConfigureSnakeCase(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    }
}
