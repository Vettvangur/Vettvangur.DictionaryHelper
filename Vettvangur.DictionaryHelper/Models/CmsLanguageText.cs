using NPoco;

namespace DictionaryHelper.Models;

[TableName("cmsLanguageText")]
[PrimaryKey("pk", AutoIncrement = true)]
public class CmsLanguageText
{
    public int pk { get; set; }
    public int languageId { get; set; }
    public Guid UniqueId { get; set; }
    public string value { get; set; }
}
