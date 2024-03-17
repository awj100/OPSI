namespace Opsi.Pocos.History;

public readonly record struct GroupedResourceVersion(string Path, IEnumerable<ResourceVersion> ResourceVersions);
