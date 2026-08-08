namespace Restaurant.Models
{
    public class RestaurantInfo
    {
        public int Id { get; set; }
        public string AddressKa { get; set; } = string.Empty;
        public string AddressEn { get; set; } = string.Empty;
        public string AddressRu { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string WorkingHoursKa { get; set; } = string.Empty;
        public string WorkingHoursEn { get; set; } = string.Empty;
        public string WorkingHoursRu { get; set; } = string.Empty;
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? MapEmbedUrl { get; set; }
    }
}
