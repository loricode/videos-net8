using ApplicationCore.Interfaces.Course;
using DomainCore.DTOs.Course;
using InfrastructureCore.Persistence.Course;

namespace ApplicationCore.Querys.Course
{
    public class CourseStats : ICourseStats
    {
        private readonly ICourseRepository _courseRepository;

        public CourseStats(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public Task<CourseStatsDto> StatsAsync(Guid userId, CancellationToken cancellationToken)
            => _courseRepository.StatsAsync(userId, cancellationToken);
    }
}
