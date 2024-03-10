namespace Opsi.Constants;

public static class HttpClientNames
{
    /// <summary>
    /// The name for a <see cref="HttpClient"/> used for calling other HTTP-triggered OPSI functions using a one-time authentication key.
    /// </summary>
    public const string OneTimeAuth = nameof(OneTimeAuth);

    /// <summary>
    /// The name for a <see cref="HttpClient"/> used for calling other HTTP-triggered OPSI functions when a context user has been authenticated.
    /// </summary>
    public const string SelfWithContextAuth = nameof(SelfWithContextAuth);

    /// <summary>
    /// The name for a <see cref="HttpClient"/> used for calling other HTTP-triggered OPSI functions when <b>no</b> context user has been authenticated.
    /// </summary>
    public const string SelfWithoutAuth = nameof(SelfWithoutAuth);
}
