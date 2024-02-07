namespace Opsi.Pocos;

public record class ResourceContent(string Name,
                                    byte[] Contents,
                                    long Length,
                                    string ContentType,
                                    DateTimeOffset LastModified,
                                    string Etag);