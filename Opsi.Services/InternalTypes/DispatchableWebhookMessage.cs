using System.Reflection;
using Opsi.Pocos;

namespace Opsi.Services.InternalTypes;

public class DispatchableWebhookMessage : WebhookMessage
{
    public static DispatchableWebhookMessage FromWebhookMessage(WebhookMessage webhookMessage, Dictionary<string, object> customProps)
    {
        var dispatchableWebhookMessage = new DispatchableWebhookMessage { CustomProps = customProps };

        foreach (var propInfo in webhookMessage.GetType().GetProperties(BindingFlags.Instance| BindingFlags.Public))
        {
            propInfo.SetValue(dispatchableWebhookMessage, propInfo.GetValue(webhookMessage));
        }

        return dispatchableWebhookMessage;
    }

    public Dictionary<string, object>? CustomProps { get; set; }
}
