namespace DictionaryHelper.Models;

public class DictionaryItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public Guid Parent { get; set; }
}
