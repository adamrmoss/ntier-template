using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using NTierTemplate.Application.Ioc;

namespace NTierTemplate.Test.Support;

/// <summary>
/// Builds named <see cref="JsonSerializerOptions"/> monitors for controller and queue tests.
/// </summary>
public static class TestJsonOptionsMonitor
{
    /// <summary>
    /// Create a monitor that returns camelCase options for frontend JSON tests.
    /// </summary>
    public static IOptionsMonitor<JsonSerializerOptions> CreateCamelCaseMonitor()
    {
        return CreateMonitor(SerializerRegistrar.CamelCaseOptionsName, SerializerRegistrar.ConfigureCamelCase);
    }

    /// <summary>
    /// Create a monitor that returns PascalCase options for queue JSON tests.
    /// </summary>
    public static IOptionsMonitor<JsonSerializerOptions> CreatePascalCaseMonitor()
    {
        return CreateMonitor(SerializerRegistrar.PascalCaseOptionsName, SerializerRegistrar.ConfigurePascalCase);
    }

    private static IOptionsMonitor<JsonSerializerOptions> CreateMonitor(
        string optionsName,
        Action<JsonSerializerOptions> configure
    )
    {
        var options = new JsonSerializerOptions();
        configure(options);

        var monitor = new Mock<IOptionsMonitor<JsonSerializerOptions>>();
        monitor.Setup(m => m.Get(optionsName)).Returns(options);
        return monitor.Object;
    }
}
