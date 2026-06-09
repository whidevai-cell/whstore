using System.ComponentModel.DataAnnotations;

namespace whstore.Models
{
    public class EmbedModel
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string EmbedUrl { get; set; } = "";
        public string EmbedType { get; set; } = "YouTube";
        public string? Description { get; set; }
        public bool IsVisible { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
