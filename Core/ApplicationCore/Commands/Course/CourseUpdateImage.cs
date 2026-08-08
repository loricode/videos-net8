using ApplicationCore.Interfaces.Course;
using InfrastructureCore.Persistence.Course;

namespace ApplicationCore.Commands.Course
{
    public class CourseUpdateImage : ICourseUpdateImage
    {
        private readonly ICourseRepository _courseRepository;
        public CourseUpdateImage(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }
        public Task UpdateImageByIdAsync(Guid courseId, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken)
        {
            return _courseRepository.UpdateImageByIdAsync(courseId, userId, stream, fileName, cancellationToken);
        }
    }
}
