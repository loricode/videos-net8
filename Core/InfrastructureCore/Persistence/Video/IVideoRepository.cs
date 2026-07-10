
using DomainCore.DTOs.Video;

namespace InfrastructureCore.Persistence.Video
{
    public interface IVideoRepository
    {
        Task<Guid> AddAsync(Stream stream, string fileName, string title, string description, Guid courseId, CancellationToken cancellationToken);

        Task<IEnumerable<VideoDto>> ByCourseIdAsync(Guid courseId, CancellationToken cancellationToken);

        Task<VideoDetailDto> FromCourseIdAsync(Guid courseId, CancellationToken cancellationToken);

    }
}
