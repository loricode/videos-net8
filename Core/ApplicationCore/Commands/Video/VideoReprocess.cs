using ApplicationCore.Interfaces.Video;
using InfrastructureCore.Persistence.Video;

namespace ApplicationCore.Commands.Video
{
    public class VideoReprocess : IVideoReprocess
    {
        private readonly IVideoRepository _videoRepository;

        public VideoReprocess(IVideoRepository videoRepository)
        {
            _videoRepository = videoRepository;
        }

        public Task ReprocessAsync(Guid videoId, Guid userId, CancellationToken cancellationToken)
            => _videoRepository.ReprocessAsync(videoId, userId, cancellationToken);
    }
}
