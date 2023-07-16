namespace Opsi.Services.Auth;

internal class AuthHandlerProvider : IAuthHandlerProvider
{
    private readonly IReadOnlyCollection<Type> _authHandlerTypes = new List<Type>
    {
        typeof(OneTimeKeyAuthHandler),
        typeof(ReferenceAuthHandler)
    };
    private readonly Func<Type, IAuthHandler?> _typeResolver;

    public AuthHandlerProvider(Func<Type, IAuthHandler?> typeResolver)
    {
        _typeResolver = typeResolver;
    }

    public IReadOnlyCollection<IAuthHandler> GetAuthHandlers()
    {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return _authHandlerTypes.Select(authHandlerType => _typeResolver(authHandlerType))
                                .Where(resolvedInstance => resolvedInstance != null)
                                .ToList();
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }
}
