using System.Text.RegularExpressions;

namespace PvWhisper.Text;

public static class RegexReplacerExtensions
{
    public static string RegexReplace(this string input, IRegexReplacer replacer, string pattern, string replaceTemplate, RegexOptions options = RegexOptions.None)
        => replacer.RegexReplace(input, pattern, replaceTemplate, options);
}