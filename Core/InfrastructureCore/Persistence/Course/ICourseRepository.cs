using DomainCore.DTOs.Course;

namespace InfrastructureCore.Persistence.Course
{
    /// <summary>Todas las operaciones de gestión filtran por el dueño (userId del JWT).</summary>
    public interface ICourseRepository
    {
        Task<Guid> SaveAsync(string title, string description, bool state, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken);
        Task<IEnumerable<CourseDto>> ByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task<IEnumerable<CourseDetailDto>> SearchAsync(string text, CancellationToken cancellationToken);
        Task DeleteByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken);
        Task UpdateByIdAsync(Guid courseId, string title, string description, bool state, Guid userId, CancellationToken cancellationToken);
        Task<CourseDto> GetByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken);
        Task UpdateImageByIdAsync(Guid courseId, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken);
        Task<CourseStatsDto> StatsAsync(Guid userId, CancellationToken cancellationToken);
        Task<bool> IsOwnerAsync(Guid courseId, Guid userId, CancellationToken cancellationToken);
    }
}
