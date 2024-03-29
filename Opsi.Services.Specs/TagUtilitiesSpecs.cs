using FluentAssertions;

namespace Opsi.Services.Specs;

[TestClass]
public class TagUtilitiesSpecs
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private TagUtilities _testee;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [TestInitialize]
    public void TestInit()
    {
        _testee = new TagUtilities();
    }

    [TestMethod]
    public void GetSafeTagValue_WhenInputIsNull_ReturnsEmptyString()
    {
        string? input = null;

        var result = _testee.GetSafeTagValue(input);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetSafeTagValue_WhenInputIsSafeString_ReturnsSameString()
    {
        const string input = "A SAFE STRING a safe string 0123456789 + - . / : = _";

        var result = _testee.GetSafeTagValue(input);

        result.Should().Be(input);
    }

    [TestMethod]
    public void GetSafeTagValue_WhenInputIsUnsafeString_ReturnsSafeString()
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
            All other characters should be removed.
        */
        const string safePart = "AN UNSAFE STRING an unsafe string 0123456789+-./:=_";
        const string unsafePart = "!@£#$%^&*()[]{};\"|\\?<>,`~±§";
        var input = $"{safePart}{unsafePart}";

        var result = _testee.GetSafeTagValue(input);

        result.Should().Be(safePart);
    }
}
