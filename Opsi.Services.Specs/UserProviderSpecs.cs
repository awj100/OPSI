using FakeItEasy;
using FluentAssertions;
using Functions.Worker.ContextAccessor;
using Microsoft.Azure.Functions.Worker;
using Opsi.Common.Exceptions;

namespace Opsi.Services.Specs;

[TestClass]
public class UserProviderSpecs
{
    private const string ItemNameClaims = "Claims";
    private const string ItemNameUsername = "Username";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private FunctionContext _functionContext;
    private IFunctionContextAccessor _functionContextAccessor;
    private IDictionary<object, object> _items;
    private UserProvider _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _items = A.Fake<Dictionary<object, object>>();

        _functionContext = A.Fake<FunctionContext>();
        A.CallTo(() => _functionContext.Items).Returns(_items);

        _functionContextAccessor = A.Fake<IFunctionContextAccessor>();
        A.CallTo(() => _functionContextAccessor.FunctionContext).Returns(_functionContext);

        _testee = new UserProvider(_functionContextAccessor);
    }

    [TestMethod]
    public void Claims_WhenNoClaimsSetInFunctionContext_ReturnsEmpty()
    {
        _testee.Claims.Value
               .Should()
               .BeEmpty();
    }

    [TestMethod]
    public void Claims_WhenSetInFunctionContext_ReturnsClaims()
    {
        var claims = new List<string> { "item 1", "item 2" };

        _items = new Dictionary<object, object> { { ItemNameClaims, claims } };
        A.CallTo(() => _functionContext.Items).Returns(_items);

        _testee.Claims.Value
               .Should()
               .Contain(claims);
    }

    [TestMethod]
    public void Username_WhenNoUserSetInFunctionContext_ThrowsUnauthenticatedException()
    {
        _testee.Invoking(t => t.Username.Value)
            .Should()
            .Throw<UnauthenticatedException>();
    }

    [TestMethod]
    public void Username_WhenUserSetInFunctionContext_ReturnsUsername()
    {
        const string username = "test username";

        _items = new Dictionary<object, object> { {ItemNameUsername, username } };
        A.CallTo(() => _functionContext.Items).Returns(_items);

        _testee.Username.Value
               .Should()
               .Be(username);
    }
}
