namespace Opsi.Pocos.History;

public readonly record struct ResourceVersion(string Path,
                                              string Username,
                                              string? VersionId,
                                              int VersionIndex);