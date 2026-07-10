using ApplicationCore.Interfaces.Course;
using InfrastructureCore.Persistence.Course;

namespace ApplicationCore.Commands.Course
{
    public class CourseUpdate : ICourseUpdate
    {

        private readonly ICourseRepository _courseRepository;

        public CourseUpdate(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public Task UpdateByIdAsync(Guid courseId, string title, string description, bool state, Guid userId, CancellationToken cancellationToken)
        {
            return _courseRepository.UpdateByIdAsync(courseId, title, description, state, userId, cancellationToken);
        }
    }
}
