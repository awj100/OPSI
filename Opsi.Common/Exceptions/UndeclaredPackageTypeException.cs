namespace Opsi.Common.Exceptions;

public class UndeclaredPackageTypeException : Exception
{
    public UndeclaredPackageTypeException(string packageTypeIndentifier) : base($"No package handler has been declared for \"{packageTypeIndentifier}\".")
    {
    }
}
