
namespace DomainCore.DTOs.Course
{
    public class CourseDto
    {

        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool State {  get; set; }

        public string? ImgUrl { get; set; }

    }
}
