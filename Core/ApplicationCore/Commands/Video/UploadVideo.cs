using ApplicationCore.Interfaces.Video;
using InfrastructureCore.Persistence.Video;

namespace ApplicationCore.Commands.Video
{
    public class UploadVideo : IUploadVideo
    {

        private readonly IVideoRepository _videoRepository;

        public UploadVideo(IVideoRepository videoRepository)
        {
            _videoRepository = videoRepository;
        }

        public async Task<Guid> UploadAsync(Stream stream, string fileName, string title, string description, Guid courseId, CancellationToken cancellationToken)
        {
            return await _videoRepository.AddAsync(stream, fileName, title, description, courseId, cancellationToken);
        }
    }
}
