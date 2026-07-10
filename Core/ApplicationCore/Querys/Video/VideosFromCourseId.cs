using ApplicationCore.Interfaces.Video;
using DomainCore.DTOs.Video;
using InfrastructureCore.Persistence.Video;

namespace ApplicationCore.Querys.Video
{
    public class VideosFromCourseId : IVideosFromCourseId
    {

        private readonly IVideoRepository _videoRepository;
        public VideosFromCourseId(IVideoRepository videoRepository)
        {
            _videoRepository = videoRepository;
        }
        public async  Task<VideoDetailDto> FromCourseIdAsync(Guid courseId, CancellationToken cancellationToken)
        {
            return await _videoRepository.FromCourseIdAsync(courseId, cancellationToken); 
        }
    }
}
