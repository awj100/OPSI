using Opsi.Pocos;

namespace Opsi.Services;

public interface IQueueService
{
    Task AddMessageAsync(Object obj);
}