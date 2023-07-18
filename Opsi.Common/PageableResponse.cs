namespace Opsi.Common;

public readonly struct PageableResponse<T>
{
    public PageableResponse(IReadOnlyList<T> items, string? continuationToken = null)
    {
        ContinuationToken = continuationToken;
        Items = items;
    }

    public string? ContinuationToken { get; }

    public IReadOnlyList<T> Items { get; }
}
