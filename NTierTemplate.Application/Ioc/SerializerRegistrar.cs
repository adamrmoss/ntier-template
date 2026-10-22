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
    /// Register named <see cref="JsonSerializerOptions"/> for camelCase, PascalCase, and snake_case serialization.
    /// </summary>
    /// <param name="serviceCollection">The service collection to register serializers with.</param>
    public static void RegisterSerializers(this IServiceCollection serviceCollection)
    {
        serviceCollection.Configure<JsonSerializerOptions>(
            JsonSerializerOptionsName.CamelCaseOptions.ToString(),
            ConfigureCamelCase
        );
        serviceCollection.Configure<JsonSerializerOptions>(
            JsonSerializerOptionsName.PascalCaseOptions.ToString(),
            ConfigurePascalCase
        );
        serviceCollection.Configure<JsonSerializerOptions>(
            JsonSerializerOptionsName.SnakeCaseOptions.ToString(),
            ConfigureSnakeCase
        );
    }

    /// <summary>
    /// Apply camelCase JSON settings for frontend HTTP payloads.
    /// </summary>
    /// <param name="options">The serializer options to configure.</param>
    public static void ConfigureCamelCase(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Write enum member names as JSON strings.
        options.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>
    /// Apply PascalCase JSON settings for server and queue payloads.
    /// </summary>
    /// <param name="options">The serializer options to configure.</param>
    public static void ConfigurePascalCase(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = null;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Write enum member names as JSON strings.
        options.Converters.Add(new JsonStringEnumConverter());
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

        // Write enum member names as JSON strings.
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
