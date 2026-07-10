using DomainCore.DTOs.Video;

namespace ApplicationCore.Interfaces.Video
{
    public interface IVideosFromCourseId
    {
        Task<VideoDetailDto> FromCourseIdAsync(Guid courseId, CancellationToken cancellationToken);
    }
}
