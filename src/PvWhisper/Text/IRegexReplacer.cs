using System.Text.RegularExpressions;

namespace PvWhisper.Text;

public interface IRegexReplacer
{
    string RegexReplace(string input, string pattern, string replaceTemplate, RegexOptions options = RegexOptions.None);
}