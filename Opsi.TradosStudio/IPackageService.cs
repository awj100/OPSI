using Opsi.Common;
using System.Threading.Tasks;

namespace Opsi.TradosStudio
{
    public interface IPackageService: IDisposable
    {
        IReadOnlyCollection<string> GetFilePathsFromPackage();

        Task<Stream?> GetContentsAsync(string fullName);
    }
}