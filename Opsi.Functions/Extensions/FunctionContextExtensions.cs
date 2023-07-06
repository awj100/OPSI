using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Opsi.Functions.Extensions;

public static class FunctionContextExtensions
{
    /// <summary>
    /// Configures the <see cref="T:Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder" /> to use the provided middleware type, when the targeted function has been triggered by an HTTP request.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder" /> to configure.</param>
    /// <returns>The same instance of the <see cref="T:Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder" /> for chaining.</returns>
    public static IFunctionsWorkerApplicationBuilder UseWhenHttpTriggered<T>(this IFunctionsWorkerApplicationBuilder builder) where T : class, IFunctionsWorkerMiddleware
    {
        Func<FunctionContext, bool> predicate = IsHttpTrigger;
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        builder.Services.AddSingleton<T>();
        builder.Use(delegate (FunctionExecutionDelegate next)
        {
            FunctionExecutionDelegate next2 = next;
            return (FunctionContext context) => predicate(context) ? context.InstanceServices.GetRequiredService<T>().Invoke(context, next2) : next2(context);
        });
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="T:Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder" /> to use the provided middleware type, when the targeted function has <b>NOT</b> been triggered by an HTTP request.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder" /> to configure.</param>
    /// <returns>The same instance of the <see cref="T:Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder" /> for chaining.</returns>
    public static IFunctionsWorkerApplicationBuilder UseWhenNotHttpTriggered<T>(this IFunctionsWorkerApplicationBuilder builder) where T : class, IFunctionsWorkerMiddleware
    {
        Func<FunctionContext, bool> predicate = IsNonHttpTrigger;
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        builder.Services.AddSingleton<T>();
        builder.Use(delegate (FunctionExecutionDelegate next)
        {
            FunctionExecutionDelegate next2 = next;
            return (FunctionContext context) => predicate(context) ? context.InstanceServices.GetRequiredService<T>().Invoke(context, next2) : next2(context);
        });
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="T:Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder" /> to use the provided middleware type, when the targeted function has been triggered by a queue.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder" /> to configure.</param>
    /// <returns>The same instance of the <see cref="T:Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder" /> for chaining.</returns>
    public static IFunctionsWorkerApplicationBuilder UseWhenQueueTriggered<T>(this IFunctionsWorkerApplicationBuilder builder) where T : class, IFunctionsWorkerMiddleware
    {
        Func<FunctionContext, bool> predicate = IsQueueTrigger;
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        builder.Services.AddSingleton<T>();
        builder.Use(delegate (FunctionExecutionDelegate next)
        {
            FunctionExecutionDelegate next2 = next;
            return (FunctionContext context) => predicate(context) ? context.InstanceServices.GetRequiredService<T>().Invoke(context, next2) : next2(context);
        });
        return builder;
    }

    private static string GetTriggerType(FunctionContext context)
    {
        return context.FunctionDefinition
                        .InputBindings
                        .Values
                        .First(a => a.Type.EndsWith("Trigger"))
                        .Type;
    }

    private static bool IsHttpTrigger(FunctionContext context)
    {
        const string httpTriggerType = "httpTrigger";

        return GetTriggerType(context) == httpTriggerType;
    }

    private static bool IsNonHttpTrigger(FunctionContext context)
    {
        return !IsHttpTrigger(context);
    }

    private static bool IsQueueTrigger(FunctionContext context)
    {
        const string queueTriggerType = "queueTrigger";

        return GetTriggerType(context) == queueTriggerType;
    }
}
