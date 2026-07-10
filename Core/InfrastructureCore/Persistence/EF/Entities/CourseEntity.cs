
namespace InfrastructureCore.Persistence.EF.Entities
{
    public class CourseEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImgUrl { get; set; }
        public bool State {  get; set; }
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public ICollection<VideoEntity> Videos { get; set; } = new List<VideoEntity>();

    }
}
