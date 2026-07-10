using DomainCore.DTOs.Course;
using DomainCore.DTOs.Video;
using InfrastructureCore.LocalStorage;
using InfrastructureCore.Persistence.EF;
using InfrastructureCore.Persistence.EF.Entities;
using InfrastructureCore.ProcessVideo;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureCore.Persistence.Video
{
    public class VideoRepository : IVideoRepository
    {

        private readonly AppDbContext _context;

        private readonly ILocalStorageService _localStorageService;

        private readonly IVideoQueue _videoQueue;

        public VideoRepository(AppDbContext context, ILocalStorageService localStorageService, IVideoQueue videoQueue)
        {
            _context = context;
            _localStorageService = localStorageService;
            _videoQueue = videoQueue;
        }

        public async Task<Guid> AddAsync(Stream stream, string fileName, string title, string description, Guid courseId, CancellationToken cancellationToken)
        {

            var video = new VideoEntity {
                Status = "Uploading",
                Title = title,
                CourseId = courseId,
                Description = description,
            };

            _context.Videos.Add(video);

            await _context.SaveChangesAsync(cancellationToken);

            var folderOptions = await _localStorageService.SaveAsync(stream, fileName, courseId, video.Id, cancellationToken);

            video.FilePath = Path.Combine(folderOptions.OutputFolder, fileName.Trim());

            await _context.SaveChangesAsync(cancellationToken);

            await _videoQueue.QueueAsync(video.Id);

            return video.Id;

        }

        public async Task<IEnumerable<VideoDto>> ByCourseIdAsync(Guid courseId, CancellationToken cancellationToken)
        {
            var result = await _context.Videos.Where(c => c.CourseId == courseId)
              .Select(item => new VideoDto
              {
                  Id = item.Id,
                  Description = item.Description ?? "",
                  Title = item.Title,
                  Status = item.Status,
                  CreateAt = item.CreatedAt,
                  StreamUrl = item.StreamUrl
              }).ToListAsync(cancellationToken);

            return result;
        }

        public async Task<VideoDetailDto> FromCourseIdAsync(Guid courseId, CancellationToken cancellationToken)
        {
            var result = await _context.Videos.Where(c => c.CourseId == courseId)
              .Select(item => new 
              {
                  item.Id,
                  item.Title,
                  item.StreamUrl,
                  TitleCourse = item.Course.Title
              }).ToListAsync(cancellationToken);

            if (result.Count == 0)
                return new VideoDetailDto();

            return new VideoDetailDto
            {
                TitleCourse = result.First().TitleCourse,
                Items = result.Select(v => new VideoItem
                {
                    Id = v.Id,
                    Title = v.Title,
                    StreamUrl = v.StreamUrl,
                }).ToList()
            };

        }
    }
}
