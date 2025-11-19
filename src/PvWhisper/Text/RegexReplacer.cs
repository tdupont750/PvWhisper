using System;
using System.Text.RegularExpressions;
using System.Globalization;

namespace PvWhisper.Text;

public sealed class RegexReplacer : IRegexReplacer
{
    /// <summary>
    /// Replaces text in <paramref name="input"/> using a regex <paramref name="pattern"/> and a custom
    /// replacement template that supports tokens like {1}, {1:ToUpper}, {2:ToLower}.
    /// </summary>
    public string RegexReplace(
        string input,
        string pattern,
        string replaceTemplate,
        RegexOptions options = RegexOptions.None)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (pattern == null) throw new ArgumentNullException(nameof(pattern));
        if (replaceTemplate == null) throw new ArgumentNullException(nameof(replaceTemplate));

        var regex = new Regex(pattern, options);

        return regex.Replace(input, match =>
        {
            // For each match, expand the template with this match's groups
            return ExpandTemplateForMatch(replaceTemplate, match);
        });
    }

    private static string ExpandTemplateForMatch(string template, Match match)
    {
        // Tokens look like: {1}, {2:ToUpper}, {3:ToLower}
        // Group 1 = index, Group 2 = operation
        var tokenRegex = new Regex(@"\{(\d+)(?::(ToUpper|ToLower))?\}",
                                   RegexOptions.Compiled);

        string result = tokenRegex.Replace(template, m =>
        {
            int groupIndex = int.Parse(m.Groups[1].Value);
            if (groupIndex < 0 || groupIndex >= match.Groups.Count)
            {
                // Out-of-range group index – return empty or throw, depending on what you want
                return string.Empty;
            }

            string value = match.Groups[groupIndex].Value;

            if (!m.Groups[2].Success)
            {
                // No transform specified – just return the raw group value
                return value;
            }

            string op = m.Groups[2].Value;
            return op switch
            {
                "ToUpper" => value.ToUpper(CultureInfo.CurrentCulture),
                "ToLower" => value.ToLower(CultureInfo.CurrentCulture),
                _ => value
            };
        });

        return result;
    }
}

public static class RegexReplacerExtensions
{
    public static string RegexReplace(this string input, IRegexReplacer replacer, string pattern, string replaceTemplate, RegexOptions options = RegexOptions.None)
        => replacer.RegexReplace(input, pattern, replaceTemplate, options);
}
