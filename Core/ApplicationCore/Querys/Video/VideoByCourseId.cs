using ApplicationCore.Interfaces.Video;
using DomainCore.DTOs.Video;
using InfrastructureCore.Persistence.Video;

namespace ApplicationCore.Querys.Video
{
    public class VideoByCourseId: IVideoByCourseId
    {

        private readonly IVideoRepository _videoRepo;

        public VideoByCourseId(IVideoRepository videoRepo) 
        { 
          _videoRepo = videoRepo;
        }

        public async Task<IEnumerable<VideoDto>> ByCourseIdAsync(Guid courseId, CancellationToken cancellationToken)
        {
            return await _videoRepo.ByCourseIdAsync(courseId, cancellationToken);
        }
    }
}
