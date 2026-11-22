using DomainCore.Constants;
using DomainCore.DTOs.Video;
using DomainCore.Exceptions;
using InfrastructureCore.LocalStorage;
using InfrastructureCore.Persistence.EF;
using InfrastructureCore.Persistence.EF.Entities;
using InfrastructureCore.ProcessVideo;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureCore.Persistence.Video
{
    public class VideoRepository : IVideoRepository
    {
        private const string CourseNotFound = "El curso no existe o no te pertenece.";

        private readonly AppDbContext _context;
        private readonly ILocalStorageService _localStorageService;
        private readonly IVideoQueue _videoQueue;

        public VideoRepository(AppDbContext context, ILocalStorageService localStorageService, IVideoQueue videoQueue)
        {
            _context = context;
            _localStorageService = localStorageService;
            _videoQueue = videoQueue;
        }

        private Task<bool> OwnsCourse(Guid courseId, Guid userId, CancellationToken ct) =>
            _context.Courses.AnyAsync(c => c.Id == courseId && c.UserId == userId, ct);

        public async Task<Guid> AddAsync(Stream stream, string fileName, string title, string description, Guid courseId, Guid userId, CancellationToken cancellationToken)
        {
            if (!await OwnsCourse(courseId, userId, cancellationToken))
                throw new NotFoundException(CourseNotFound);

            var video = new VideoEntity
            {
                Status = VideoStatus.Uploading,
                CreatedAt = DateTime.UtcNow,
                Title = title,
                CourseId = courseId,
                Description = description,
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var folder = await _localStorageService.SaveAsync(stream, fileName, courseId, video.Id, cancellationToken);
                video.FilePath = folder.OriginalFilePath;
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // subida interrumpida: no dejar registros huérfanos
                _context.Videos.Remove(video);
                await _context.SaveChangesAsync(CancellationToken.None);
                await _localStorageService.DeleteVideoAsync(courseId, video.Id);
                throw;
            }

            await _videoQueue.QueueAsync(video.Id);
            return video.Id;
        }

        public async Task<IEnumerable<VideoDto>> ByCourseIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken)
        {
            if (!await OwnsCourse(courseId, userId, cancellationToken))
                throw new NotFoundException(CourseNotFound);

            return await _context.Videos.AsNoTracking()
              .Where(c => c.CourseId == courseId)
              .OrderBy(item => item.CreatedAt)
              .Select(item => new VideoDto
              {
                  Id = item.Id,
                  Description = item.Description ?? "",
                  Title = item.Title,
                  Status = item.Status,
                  CreatedAt = item.CreatedAt,
                  StreamUrl = item.StreamUrl,
                  ThumbnailUrl = item.ThumbnailUrl,
                  DurationSeconds = item.DurationSeconds
              }).ToListAsync(cancellationToken);
        }

        public async Task DeleteByIdAsync(Guid courseId, Guid videoId, Guid userId, CancellationToken cancellationToken)
        {
            if (!await OwnsCourse(courseId, userId, cancellationToken))
                throw new NotFoundException(CourseNotFound);

            var deleted = await _context.Videos.Where(c => c.Id == videoId && c.CourseId == courseId)
                 .ExecuteDeleteAsync(cancellationToken);

            if (deleted == 0) throw new NotFoundException("La lección no existe.");

            await _localStorageService.DeleteVideoAsync(courseId, videoId);
        }

        public async Task ReprocessAsync(Guid videoId, Guid userId, CancellationToken cancellationToken)
        {
            var video = await _context.Videos
                .FirstOrDefaultAsync(v => v.Id == videoId && v.Course.UserId == userId, cancellationToken)
                ?? throw new NotFoundException("La lección no existe o no te pertenece.");

            if (video.Status != VideoStatus.Failed)
                throw new ConflictException("Solo se pueden reintentar lecciones con error.");

            if (string.IsNullOrEmpty(video.FilePath) || !File.Exists(video.FilePath))
                throw new ConflictException("El archivo original ya no existe; vuelve a subir la lección.");

            video.Status = VideoStatus.Uploading;
            await _context.SaveChangesAsync(cancellationToken);
            await _videoQueue.QueueAsync(video.Id);
        }

        public async Task<VideoDetailDto> FromCourseIdAsync(Guid courseId, CancellationToken cancellationToken)
        {
            var course = await _context.Courses.AsNoTracking()
                .Where(c => c.Id == courseId && c.State)
                .Select(c => new { c.Title, c.Description })
                .FirstOrDefaultAsync(cancellationToken);

            if (course is null) return new VideoDetailDto();

            var items = await _context.Videos.AsNoTracking()
              .Where(v => v.CourseId == courseId && v.Status == VideoStatus.Ready)
              .OrderBy(v => v.CreatedAt)
              .Select(v => new VideoItem
              {
                  Id = v.Id,
                  Title = v.Title,
                  Description = v.Description,
                  StreamUrl = v.StreamUrl,
                  ThumbnailUrl = v.ThumbnailUrl,
                  DurationSeconds = v.DurationSeconds
              }).ToListAsync(cancellationToken);

            return new VideoDetailDto
            {
                TitleCourse = course.Title,
                DescriptionCourse = course.Description ?? "",
                Items = items
            };
        }
    }
}
