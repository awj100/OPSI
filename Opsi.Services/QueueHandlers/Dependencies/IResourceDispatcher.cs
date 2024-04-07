namespace Opsi.Services.QueueHandlers.Dependencies;

public interface IResourceDispatcher
{
    Task<HttpResponseMessage> DispatchAsync(string hostUrl,
                                            Guid projectId,
                                            string filePath,
                                            Stream contentsStream,
                                            string username,
                                            bool isAdministrator);
}
