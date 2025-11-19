using PvWhisper.Text;

// ReSharper disable ConvertToConstant.Local

namespace PvWhisper.Tests;

public class RegexReplacerTests
{
    private static readonly IRegexReplacer Replacer = new RegexReplacer();

    [Fact]
    public void ReplaceWithGroups_UppercasesFirstCharAfterSpace()
    {
        // Arrange
        var input   = "Space hello world";
        var pattern = @"^Space\s(\w)";
        var replace = "{1:ToUpper}";

        // Act
        var result = input.RegexReplace(Replacer, pattern, replace);

        // Assert
        Assert.Equal("Hello world", result);
    }

    [Fact]
    public void ReplaceWithGroups_LowercasesFirstCharAfterTag()
    {
        // Arrange
        var input   = "[TAG] Hello World";
        var pattern = @"^\[TAG\]\s+(\w)";
        var replace = "{1:ToLower}";

        // Act
        var result = input.RegexReplace(Replacer, pattern, replace);

        // Assert
        Assert.Equal("hello World", result);
    }

    [Fact]
    public void ReplaceWithGroups_UsesMultipleGroupsAndTransforms()
    {
        // Arrange
        var input   = "Name: JOHN DOE";
        var pattern = @"Name:\s+(\w+)\s+(\w+)";
        var replace = "{1:ToLower} {2:ToUpper}";

        // Act
        var result = input.RegexReplace(Replacer, pattern, replace);

        // Assert
        Assert.Equal("john DOE", result);
    }

    [Fact]
    public void ReplaceWithGroups_NoTransform_JustInsertsGroup()
    {
        // Arrange
        var input   = "abc123";
        var pattern = @"([a-z]+)(\d+)";
        var replace = "{2}-{1}";

        // Act
        var result = input.RegexReplace(Replacer, pattern, replace);

        // Assert
        Assert.Equal("123-abc", result);
    }

    [Fact]
    public void ReplaceWithGroups_WhenNoMatch_ReturnsOriginalInput()
    {
        // Arrange
        var input   = "no match here";
        var pattern = @"^Space\s(\w)";
        var replace = "{1:ToUpper}";

        // Act
        var result = input.RegexReplace(Replacer, pattern, replace);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void ReplaceWithGroups_OutOfRangeGroupIndex_ReturnsEmptyForThatToken()
    {
        // Arrange
        var input   = "abc123";
        var pattern = @"([a-z]+)(\d+)";
        var replace = "{3}-{1}"; // group 3 does not exist

        // Act
        var result = input.RegexReplace(Replacer, pattern, replace);

        // Assert
        // {3} becomes empty string, {1} is "abc"
        Assert.Equal("-abc", result);
    }

    [Fact]
    public void ReplaceWithGroups_ThrowsOnNullArguments()
    {
        // pattern
        Assert.Throws<ArgumentNullException>(() =>
            "input".RegexReplace(Replacer, null!, "{1}"));

        // input
        Assert.Throws<ArgumentNullException>(() =>
            Replacer.RegexReplace(null!, "pattern", "{1}"));

        // template
        Assert.Throws<ArgumentNullException>(() =>
            "input".RegexReplace(Replacer, "pattern", null!));
    }
}