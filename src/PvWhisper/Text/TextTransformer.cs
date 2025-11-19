using System.Text.RegularExpressions;

namespace PvWhisper.Text;

public sealed class TextTransformer
{
    private readonly List<TextTransformConfig> _transforms;

    public TextTransformer(IEnumerable<TextTransformConfig>? transforms)
    {
        _transforms = transforms?.Where(t => t != null).ToList() ?? new List<TextTransformConfig>();
    }
    
    public string Transform(string text)
    {
        if (string.IsNullOrEmpty(text) || _transforms.Count == 0)
            return text;

        var result = text;

        foreach (var t in _transforms)
        {
            if (t == null) continue;
            var find = t.Find;
            var replace = t.Replace ?? string.Empty;
            if (string.IsNullOrEmpty(find)) continue;

            if (t.IsRegex)
            {
                var options = t.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                // Use RegexReplacer to support advanced replacement templates
                result = result.RegexReplace(find, replace, options);
            }
            else
            {
                var comparison = t.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                result = result.Replace(find, replace, comparison);
            }
        }

        return result;
    }
}