namespace Restaurant.Models
{
    public class GalleryPhoto
    {
        public int Id { get; set; }
        public string TitleKa { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
        public string TitleRu { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
