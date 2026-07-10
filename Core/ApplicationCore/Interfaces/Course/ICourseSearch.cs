using DomainCore.DTOs.Course;


namespace ApplicationCore.Interfaces.Course
{
    public interface ICourseSearch
    {

        Task<IEnumerable<CourseDetailDto>> SearchAsync(string text, CancellationToken cancellationToken);

    }
}
