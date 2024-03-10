using System.Reflection;
using FluentAssertions;
using Opsi.Services.Auth;

namespace Opsi.Services.Specs.Auth;

[TestClass]
public class AuthHandlerProviderSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Func<Type, IAuthHandler?> _typeResolver;
    private AuthHandlerProvider _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _typeResolver = type => {
            if (typeof(IAuthHandler).IsAssignableFrom(type))
            {
                return (IAuthHandler)Activator.CreateInstance(type)!;
            }

            return null;
        };

        _testee = new AuthHandlerProvider(_typeResolver);
    }

    [TestMethod]
    public void GetAuthHandlers_ReturnsRegisteredInstances()
    {
        // Arrange
        var testAuthHandlers = new List<Type> {
            typeof(AuthHandler1),
            typeof(AuthHandler2)
        };

        AssignTypesToInternalField(testAuthHandlers);

        // Assign
        var resolvedInstances = _testee.GetAuthHandlers();

        // Assert
        resolvedInstances.Should().NotBeNull()
            .And.HaveCount(testAuthHandlers.Count);
        resolvedInstances.ElementAt(0).Should().BeOfType<AuthHandler1>();
        resolvedInstances.ElementAt(1).Should().BeOfType<AuthHandler2>();
    }

    [TestMethod]
    public void GetAuthHandlers_WhenNonIAuthHandlerIsRegistered_IgnoresNonIAuthHandler()
    {
        // Arrange
        var testAuthHandlers = new List<Type> {
            typeof(AuthHandler1),
            typeof(AuthHandler2),
            typeof(InvalidAuthHandler)
        };

        AssignTypesToInternalField(testAuthHandlers);

        // Assign
        var resolvedInstances = _testee.GetAuthHandlers();

        // Assert
        resolvedInstances.Should().NotBeNull()
            .And.HaveCount(2);
        resolvedInstances.ElementAt(0).Should().BeOfType<AuthHandler1>();
        resolvedInstances.ElementAt(1).Should().BeOfType<AuthHandler2>();
    }

    private void AssignTypesToInternalField(IReadOnlyCollection<Type> testAuthHandlers)
    {
        var fieldAuthHandlers = typeof(AuthHandlerProvider).GetField("_authHandlerTypes", BindingFlags.Instance | BindingFlags.NonPublic);

        fieldAuthHandlers?.SetValue(_testee, testAuthHandlers);
    }

    private class AuthHandler1 : IAuthHandler
    {
        public Task<bool> AuthenticateAsync(string authHeader, IDictionary<object, object> contextItems)
        {
            throw new NotImplementedException();
        }
    }

    private class AuthHandler2 : IAuthHandler
    {
        public Task<bool> AuthenticateAsync(string authHeader, IDictionary<object, object> contextItems)
        {
            throw new NotImplementedException();
        }
    }

    private class InvalidAuthHandler
    {
    }
}
