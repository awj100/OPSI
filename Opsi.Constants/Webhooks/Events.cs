namespace Opsi.Constants.Webhooks;

public static class Events
{
    public static readonly string AlreadyExists = nameof(AlreadyExists);
    public static readonly string Locked = nameof(Locked);
    public static readonly string StateChange = nameof(StateChange);
    public static readonly string Stored = nameof(Stored);
    public static readonly string StoreFailure = nameof(StoreFailure);
    public static readonly string Unlocked = nameof(Unlocked);
    public static readonly string Uploaded = nameof(Uploaded);
}
