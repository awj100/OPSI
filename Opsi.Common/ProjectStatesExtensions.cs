using System.Reflection;
using Opsi.Constants;

namespace Opsi.Common;

public static class ProjectStatesExtensions
{
    /// <summary>
    /// A utility method for determining if the case-insensitive <see cref="possibleProjectState"/>
    /// is recognised as a valid value of <see cref="ProjectStates"/>.
    /// </summary>
    /// <param name="possibleProjectState"></param>
    /// <returns>
    /// An <see cref="Option{string}"/> where <c>.IsSome</c> is <c>true</c> if <see cref="possibleProjectState"/> is valid;
    /// Otherwise <see cref="Option{string}.IsNone"/> will be <c>true</c>.
    /// </returns>
    public static Option<string> GetValidProjectState(string possibleProjectState)
    {
        var projectStates = typeof(ProjectStates).GetFields()
                                                 .Select(propInfo => propInfo.Name);

        foreach(var projectState in projectStates)
        {
            if (projectState.Equals(possibleProjectState, StringComparison.OrdinalIgnoreCase))
            {
                return Option<string>.Some(projectState);
            }
        }

        return Option<string>.None();
    }
}
