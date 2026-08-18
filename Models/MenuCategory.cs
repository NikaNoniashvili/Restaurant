namespace Restaurant.Models;

public class MenuCategory
{
    public int Id { get; set; }
    public string NameKa { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameRu { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}