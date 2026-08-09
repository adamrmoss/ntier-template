using Microsoft.Extensions.DependencyInjection;
using NTierTemplate.Application.Queue;

namespace NTierTemplate.Queue.Ioc;

/// <summary>
/// Registers queue command handlers with the dependency injection container.
/// </summary>
public static class QueueHandlerRegistrar
{
    /// <summary>
    /// Register every concrete <see cref="CommandHandler{TCommand}"/> in the Queue assembly.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    public static void RegisterQueueCommandHandlers(this IServiceCollection serviceCollection)
    {
        var handlerBaseType = typeof(CommandHandler<>);

        // Scan the Queue assembly for concrete handler classes.
        foreach (var handlerType in typeof(QueueHandlerRegistrar).Assembly.GetTypes())
        {
            // Ignore interfaces, abstracts, and static helpers.
            if (handlerType is not { IsClass: true, IsAbstract: false })
            {
                continue;
            }

            // Keep only types that inherit CommandHandler<TCommand>.
            if (!InheritsCommandHandler(handlerType, handlerBaseType))
            {
                continue;
            }

            // Register for direct injection and for ICommandHandler dispatch.
            serviceCollection.AddScoped(handlerType);
            serviceCollection.AddScoped(typeof(ICommandHandler), handlerType);
        }
    }

    private static bool InheritsCommandHandler(Type handlerType, Type handlerBaseType)
    {
        // Walk the inheritance chain looking for CommandHandler<TCommand>.
        for (var current = handlerType.BaseType; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == handlerBaseType)
            {
                return true;
            }
        }

        return false;
    }
}
