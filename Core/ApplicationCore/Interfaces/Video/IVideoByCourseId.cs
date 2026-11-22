using DomainCore.DTOs.Video;

namespace ApplicationCore.Interfaces.Video
{
    public interface IVideoByCourseId
    {
        Task<IEnumerable<VideoDto>> ByCourseIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken);

    }
}
