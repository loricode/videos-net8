using ApplicationCore.Interfaces.Course;
using DomainCore.DTOs.Course;
using InfrastructureCore.Persistence.Course;

namespace ApplicationCore.Querys.Course
{
    public class CourseByUserId : ICourseByUserId
    {

        private readonly ICourseRepository _courseRepo;

        public CourseByUserId(ICourseRepository courseRepo)
        {
            _courseRepo = courseRepo;
        }

        public async Task<IEnumerable<CourseDto>> ByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {

            return await _courseRepo.ByUserIdAsync(userId, cancellationToken);
            
        }
    }
}
