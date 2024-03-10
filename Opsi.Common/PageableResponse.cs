namespace Opsi.Common;

public struct PageableResponse<T>
{
    public PageableResponse()
    {
        ContinuationToken = null;
        Items = new List<T>();
    }

    public PageableResponse(List<T> items, string? continuationToken = null)
    {
        ContinuationToken = continuationToken;
        Items = items;
    }

    public string? ContinuationToken { get; set; }

    public List<T> Items { get; set; }

    public override readonly string ToString()
    {
        return $"{Items.Count} {typeof(T).Name} instances";
    }
}
