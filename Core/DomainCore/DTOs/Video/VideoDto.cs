namespace DomainCore.DTOs.Video
{
    public class VideoDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public double? DurationSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
