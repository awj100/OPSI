using System.Text;

namespace Opsi.Services.KeyPolicies;

public abstract class KeyPoliciesBase
{
    protected static string GetAlphanumericallySubstitutedString(string s)
    {
        var lowerInvariantS = s.ToLowerInvariant();
        // Yes, we *could* handle the to-lower-case in the below foreach, but the above method
        // also handles the switch to invariant.

        const int charA = 'a';
        const int charZ = 'z';
        const int char0 = '0';
        const int char9 = '9';

        var newString = new StringBuilder();

        foreach (var c in lowerInvariantS)
        {
            if (c >= char0 && c <= char9)
            {
                newString.Append((char)(char0 + char9 - c));
            }
            else if (c >= charA && c <= charZ)
            {
                newString.Append((char)(charA + charZ - c));
            }

            newString.Append(c);
        }

        return newString.ToString();
    }

    protected static string GetUniqueOrderPart(bool forAscendingKey)
    {
        return string.Format("{0:D19}", forAscendingKey
                                            ? HiResDateTime.UtcNowTicks
                                            : DateTime.MaxValue.Ticks - HiResDateTime.UtcNowTicks);
    }
}
