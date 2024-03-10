using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Opsi.Services.QueueServices;

/// <summary>
/// <para>Passes errors into a channel from which they can be sent to subscribers, stored for statistics, etic.</para>
/// <para>Implementations of this interface are not expected to send notifications.</para>
/// </summary>
public interface IErrorQueueService
{
    Task ReportAsync(Exception exception,
                     LogLevel logLevel = LogLevel.Error,
                     [CallerMemberName] string exceptionOrigin = "");
}
