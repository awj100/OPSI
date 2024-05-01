using FakeItEasy;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Opsi.Common.Exceptions;
using Opsi.Functions.Middleware;
using Opsi.Services;

namespace Opsi.Functions.Specs.Middleware;

[TestClass]
public class AdministratorEnforcementSpecs
{
    private const string Username = "user@test.com";
    private readonly FunctionExecutionDelegate _functionExecutionDelegate = new(_ => Task.CompletedTask);
    private readonly ILoggerFactory _logger = NullLoggerFactory.Instance;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private FunctionContext _functionContext;
    private FunctionDefinition _functionDefinition;
    private Func<FunctionContext, IUserProvider> _funcUserProvider;
    private IUserProvider _userProvider;
    private AdministratorEnforcement _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _functionContext = A.Fake<FunctionContext>();
        _functionDefinition = A.Fake<FunctionDefinition>();
        _funcUserProvider = _ => _userProvider;
        _userProvider = A.Fake<IUserProvider>();

        A.CallTo(() => _functionContext.FunctionDefinition).Returns(_functionDefinition);
    }

    [TestMethod]
    public async Task Invoke_WhenEntryPointIsInAdministratorNamespaceAndUserIsAdministrator_CallsNext()
    {
        const bool isAdministrator = true;

        var entryPoint = "Opsi.Functions.Functions.Administrator.Something";

        A.CallTo(() => _functionDefinition.EntryPoint).Returns(entryPoint);
        A.CallTo(() => _userProvider.IsAdministrator).Returns(isAdministrator);
        A.CallTo(() => _userProvider.Username).Returns(Username);

        _testee = new AdministratorEnforcement(_funcUserProvider, _logger);

        await _testee.Invoke(_functionContext, _functionExecutionDelegate);
    }

    [TestMethod]
    public async Task Invoke_WhenEntryPointIsInAdministratorNamespaceAndUserIsNotAdministrator_ThrowsUnauthenticatedException()
    {
        const bool isAdministrator = false;

        var entryPoint = "Opsi.Functions.Functions.Administrator.Something";

        A.CallTo(() => _functionDefinition.EntryPoint).Returns(entryPoint);
        A.CallTo(() => _userProvider.IsAdministrator).Returns(isAdministrator);
        A.CallTo(() => _userProvider.Username).Returns(Username);

        _testee = new AdministratorEnforcement(_funcUserProvider, _logger);

        await _testee.Invoking(t => t.Invoke(_functionContext, _functionExecutionDelegate))
                     .Should()
                     .ThrowAsync<UnauthenticatedException>();
    }

    [TestMethod]
    public async Task Invoke_WhenEntryPointIsNotInAdministratorNamespaceAndUserIsAdministrator_CallsNext()
    {
        const bool isAdministrator = true;

        var entryPoint = "Opsi.Functions.Functions.Something";

        A.CallTo(() => _functionDefinition.EntryPoint).Returns(entryPoint);
        A.CallTo(() => _userProvider.IsAdministrator).Returns(isAdministrator);
        A.CallTo(() => _userProvider.Username).Returns(Username);

        _testee = new AdministratorEnforcement(_funcUserProvider, _logger);

        await _testee.Invoke(_functionContext, _functionExecutionDelegate);
    }

    [TestMethod]
    public async Task Invoke_WhenEntryPointIsNotInAdministratorNamespaceAndUserIsNotAdministrator_CallsNext()
    {
        const bool isAdministrator = false;

        var entryPoint = "Opsi.Functions.Functions.Something";

        A.CallTo(() => _functionDefinition.EntryPoint).Returns(entryPoint);
        A.CallTo(() => _userProvider.IsAdministrator).Returns(isAdministrator);
        A.CallTo(() => _userProvider.Username).Returns(Username);


        _testee = new AdministratorEnforcement(_funcUserProvider, _logger);

        await _testee.Invoke(_functionContext, _functionExecutionDelegate);
    }
}
