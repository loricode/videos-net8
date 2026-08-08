using ApplicationCore.Interfaces.Video;
using InfrastructureCore.Persistence.Video;

namespace ApplicationCore.Commands.Video
{
    public class VideoDelete : IVideoDelete
    {

        private readonly IVideoRepository _videoRepository;

        public VideoDelete(IVideoRepository videoRepository)
        {
            _videoRepository = videoRepository;
        }
        public async Task DeleteByIdAsync(Guid courseId, Guid videoId, Guid userId, CancellationToken cancellationToken)
        {
            await _videoRepository.DeleteByIdAsync(courseId, videoId, userId, cancellationToken);
        }
    }
}
