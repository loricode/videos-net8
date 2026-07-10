using DomainCore.DTOs.Course;

namespace InfrastructureCore.Persistence.Course
{
    public interface ICourseRepository
    {

        Task<Guid> SaveAsync(string title, string description, bool state, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken);

        Task<IEnumerable<CourseDto>> ByUserIdAsync(Guid userId, CancellationToken cancellationToken);

        Task<IEnumerable<CourseDetailDto>> SearchAsync(string text, CancellationToken cancellationToken);
        Task DeleteByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken); 
        Task UpdateByIdAsync(Guid courseId, string title, string description, bool state, Guid userId, CancellationToken cancellationToken);
        Task<CourseDto> GetByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken);

    }
}
