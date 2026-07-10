
namespace InfrastructureCore.Persistence.EF.Entities
{
    public class VideoEntity
    {

        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid CourseId { get; set; }
        public CourseEntity Course { get; set; } = null!;

    }
}
