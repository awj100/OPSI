using Opsi.Abstractions;

namespace Opsi.Functions.FormHelpers;

#pragma warning disable CS8644 // Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn't match.
internal class FormFileCollection : Dictionary<string, Stream>, IFormFileCollection
#pragma warning restore CS8644 // Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn't match.
{
}
