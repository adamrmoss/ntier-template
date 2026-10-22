namespace NTierTemplate.Application.Ioc;

/// <summary>
/// Named JSON serializer option sets. The member name is the options name.
/// </summary>
public enum JsonSerializerOptionsName
{
    /// <summary>
    /// CamelCase JSON consumed by the frontend.
    /// </summary>
    CamelCaseOptions,

    /// <summary>
    /// PascalCase JSON used on the server and in queue messages.
    /// </summary>
    PascalCaseOptions,

    /// <summary>
    /// Snake_case JSON used by third-party integrations.
    /// </summary>
    SnakeCaseOptions,
}
