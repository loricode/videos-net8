using DomainCore.DTOs.Course;

namespace ApplicationCore.Interfaces.Course
{
    public interface ICourseByUserId
    {
        Task<IEnumerable<CourseDto>> ByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    }
}
