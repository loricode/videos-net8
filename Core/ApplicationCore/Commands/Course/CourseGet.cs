using ApplicationCore.Interfaces.Course;
using DomainCore.DTOs.Course;
using InfrastructureCore.Persistence.Course;

namespace ApplicationCore.Commands.Course
{
    public class CourseGet : ICourseGet
    {
        private readonly ICourseRepository _courseRepository;

        public CourseGet(ICourseRepository courseRepository) 
        {
          _courseRepository = courseRepository;
        }
        public Task<CourseDto> GetByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken)
        {
            return _courseRepository.GetByIdAsync(courseId, userId, cancellationToken);
        }
    }
}
