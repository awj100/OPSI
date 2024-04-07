namespace Opsi.Services;

public record class OneTimeAuthCredentials(string Username, bool IsAdministrator, bool IsValid);
