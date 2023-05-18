using System.Collections.Generic;
using System.IO;

namespace Opsi.Abstractions
{
    public interface IFormFileCollection : IDictionary<string, Stream>
    {
    }
}
