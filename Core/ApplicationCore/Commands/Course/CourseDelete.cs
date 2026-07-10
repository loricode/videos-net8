using ApplicationCore.Interfaces.Course;
using InfrastructureCore.Persistence.Course;

namespace ApplicationCore.Commands.Course
{
    public class CourseDelete : ICourseDelete
    {

        private readonly ICourseRepository _courseRepository;

        public CourseDelete(ICourseRepository courseRepository) 
        {
          _courseRepository = courseRepository;
        }
        public Task DeleteByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken)
        {
            return _courseRepository.DeleteByIdAsync(courseId, userId, cancellationToken);
        }
    }
}
