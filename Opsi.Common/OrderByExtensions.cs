using Opsi.Constants;

namespace Opsi.Common;

public static class OrderByExtensions
{
    /// <summary>
    /// A utility method for determining if the case-insensitive <see cref="possibleOrderBy"/>
    /// is recognised as a valid value of <see cref="OrderBy"/>.
    /// </summary>
    /// <param name="possibleOrderBy"></param>
    /// <returns>
    /// An <see cref="Option{string}"/> where <c>.IsSome</c> is <c>true</c> if <see cref="possibleOrderBy"/> is valid;
    /// Otherwise <see cref="Option{string}.IsNone"/> will be <c>true</c>.
    /// </returns>
    public static Option<string> GetValidOrderBy(string possibleOrderBy)
    {
        var orderBys = typeof(OrderBy).GetFields()
                                      .Select(propInfo => propInfo.Name);

        foreach(var orderBy in orderBys)
        {
            if (orderBy.Equals(possibleOrderBy, StringComparison.OrdinalIgnoreCase))
            {
                return Option<string>.Some(orderBy);
            }
        }

        return Option<string>.None();
    }
}
