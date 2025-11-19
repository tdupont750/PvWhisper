// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global
namespace PvWhisper.Text;

public class TextTransformConfig
{
    public string Description { get; set; } = string.Empty;
    public string Find { get; set; } = string.Empty;
    public string Replace { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; }
    public bool IsRegex { get; set; }
}