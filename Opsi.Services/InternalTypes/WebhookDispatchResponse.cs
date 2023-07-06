namespace Opsi.Services.InternalTypes;

public class WebhookDispatchResponse
{
    public bool IsSuccessful { get; set; }

    public string? FailureReason { get; set; }
}
