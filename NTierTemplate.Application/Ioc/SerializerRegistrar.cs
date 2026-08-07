using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace NTierTemplate.Application.Ioc;

/// <summary>
/// Registers JSON serializer options (camelCase and snake_case) for the application.
/// </summary>
public static class SerializerRegistrar
{
    /// <summary>
    /// Register named <see cref="JsonSerializerOptions"/> for camelCase and snake_case serialization.
    /// </summary>
    /// <param name="serviceCollection">The service collection to register serializers with.</param>
    public static void RegisterSerializers(this IServiceCollection serviceCollection)
    {
        serviceCollection.Configure<JsonSerializerOptions>("CamelCaseOptions", options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        serviceCollection.Configure<JsonSerializerOptions>("SnakeCaseOptions", options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        });
    }
}
