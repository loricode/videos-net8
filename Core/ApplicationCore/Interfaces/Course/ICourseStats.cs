using DomainCore.DTOs.Course;

namespace ApplicationCore.Interfaces.Course
{
    public interface ICourseStats
    {
        Task<CourseStatsDto> StatsAsync(Guid userId, CancellationToken cancellationToken);
    }
}
