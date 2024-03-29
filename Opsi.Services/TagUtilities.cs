using System.Text.RegularExpressions;

namespace Opsi.Services;

internal class TagUtilities : ITagUtilities
{
    public string GetSafeTagValue(object? tagValue)
    {
        /*
            Valid tag key and value characters include
            - lower and upper case letters
            - digits (0-9)
            - space (' ')
            - plus ('+')
            - minus ('-')
            - period ('.')
            - forward slash ('/')
            - colon (':')
            - equals ('=')
            - underscore ('_')
        */

        const string defaultValue = "";
        const string prohibitedSymbolReplacement = "";

        var pattern = "[^a-zA-Z0-9 +\\-./: =_]";
        var rawString = tagValue?.ToString() ?? defaultValue;
        return Regex.Replace(rawString, pattern, prohibitedSymbolReplacement);
    }
}
