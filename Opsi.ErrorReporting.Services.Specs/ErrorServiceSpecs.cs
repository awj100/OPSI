using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Opsi.Pocos;

namespace Opsi.ErrorReporting.Services.Specs;

[TestClass]
public class ErrorServiceSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IErrorEmailService _errorEmailService;
    private IErrorStorageService _errorStorageService;
    private ILogger<ErrorService> _logger;
    private ILoggerFactory _loggerFactory;
    private ErrorService _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _errorEmailService = A.Fake<IErrorEmailService>();
        _errorStorageService = A.Fake<IErrorStorageService>();
        _logger = A.Fake<ILogger<ErrorService>>();
        _loggerFactory = A.Fake<ILoggerFactory>();

        A.CallTo(_loggerFactory)
         .Where(call => call.Method.Name == nameof(ILoggerFactory.CreateLogger) && call.Method.IsGenericMethod)
         .WithReturnType<ILogger<ErrorService>>()
         .ReturnsLazily(call => _logger);

        _testee = new ErrorService(_errorEmailService,
                                   _errorStorageService,
                                   _loggerFactory);
    }

    [TestMethod]
    public async Task ReportAsync_SendsEmail()
    {
        var error = GetPopulatedError();

        await _testee.ReportAsync(error);

        A.CallTo(() => _errorEmailService.SendAsync(error)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task ReportAsync_StoresError()
    {
        var error = GetPopulatedError();

        await _testee.ReportAsync(error);

        A.CallTo(() => _errorStorageService.StoreAsync(error)).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task ReportAsync_WhenErrorEmailServiceThrowsException_BubblesNoException()
    {
        var error = GetPopulatedError();

        A.CallTo(() => _errorEmailService.SendAsync(A<Error>._)).Throws<Exception>();

        await _testee.Invoking(t => t.ReportAsync(error))
                     .Should()
                     .NotThrowAsync<Exception>();
    }

    //[TestMethod]
    public async Task ReportAsync_WhenErrorEmailServiceThrowsException_CreatesCriticalLogEntry()
    {
        var error = GetPopulatedError();

        A.CallTo(() => _errorEmailService.SendAsync(A<Error>._)).Throws<Exception>();

        try
        {
            await _testee.ReportAsync(error);
        }
        catch(Exception)
        {
        }

        A.CallTo(() => _logger.LogCritical(A<string>.That.Matches(s => s.ToLower().Contains("send")))).MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task ReportAsync_WhenErrorStorageServiceThrowsException_BubblesNoException()
    {
        var error = GetPopulatedError();

        A.CallTo(() => _errorStorageService.StoreAsync(A<Error>._)).Throws<Exception>();

        await _testee.Invoking(t => t.ReportAsync(error))
                     .Should()
                     .NotThrowAsync<Exception>();
    }

    //[TestMethod]
    public async Task ReportAsync_WhenErrorStorageServiceThrowsException_CreatesCriticalLogEntry()
    {
        var error = GetPopulatedError();

        A.CallTo(() => _errorStorageService.StoreAsync(A<Error>._)).Throws<Exception>();

        try
        {
            await _testee.ReportAsync(error);
        }
        catch (Exception)
        {
        }

        A.CallTo(() => _logger.LogCritical(A<string>.That.Matches(s => s.ToLower().Contains("store")))).MustHaveHappenedOnceExactly();
    }

    private static Error GetPopulatedError()
    {
        var error = new Error();
        var errorType = error.GetType();

        foreach (var propInfo in errorType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                                          .Where(propInfo => propInfo.PropertyType == typeof(string)))
        {
            propInfo.SetValue(error, $"{propInfo.Name}_value");
        }

        return error;
    }
}
