namespace Restaurant.Models
{
    public class MenuItem
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string NameKa { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameRu { get; set; } = string.Empty;
        public string DescriptionKa { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public string DescriptionRu { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int SortOrder { get; set; }
    }
}
