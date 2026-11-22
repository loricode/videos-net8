using DomainCore.DTOs.Video;

namespace InfrastructureCore.Persistence.Video
{
    public interface IVideoRepository
    {
        /// <summary>Sube un video a un curso del usuario. Lanza NotFound si el curso no es suyo.</summary>
        Task<Guid> AddAsync(Stream stream, string fileName, string title, string description, Guid courseId, Guid userId, CancellationToken cancellationToken);
        Task<IEnumerable<VideoDto>> ByCourseIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken);
        /// <summary>Lecciones públicas (solo Ready de cursos activos).</summary>
        Task<VideoDetailDto> FromCourseIdAsync(Guid courseId, CancellationToken cancellationToken);
        Task DeleteByIdAsync(Guid courseId, Guid videoId, Guid userId, CancellationToken cancellationToken);
        /// <summary>Vuelve a encolar un video en estado Failed que conserva su original.</summary>
        Task ReprocessAsync(Guid videoId, Guid userId, CancellationToken cancellationToken);
    }
}
