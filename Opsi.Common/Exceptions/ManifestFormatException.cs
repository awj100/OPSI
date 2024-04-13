namespace Opsi.Common.Exceptions;

public class ManifestFormatException : Exception
{
    public ManifestFormatException() : base("The provided content is not recognised as a correctly-formatted manifest.")
    {
    }
}
