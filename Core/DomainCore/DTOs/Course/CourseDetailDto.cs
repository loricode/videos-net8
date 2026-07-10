
namespace DomainCore.DTOs.Course
{
    public class CourseDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImgUrl { get; set; }

    }
}
