using DomainCore.Constants;
using DomainCore.DTOs.Course;
using DomainCore.Exceptions;
using InfrastructureCore.LocalStorage;
using InfrastructureCore.ObjectStorage;
using InfrastructureCore.Persistence.EF;
using InfrastructureCore.Persistence.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureCore.Persistence.Course
{
    public class CourseRepository : ICourseRepository
    {
        private const string NotFound = "El curso no existe o no te pertenece.";

        private readonly AppDbContext _context;
        private readonly ILocalStorageService _storageService;
        private readonly IObjectStorage _objectStorage;

        public CourseRepository(AppDbContext context, ILocalStorageService storageService, IObjectStorage objectStorage)
        {
            _context = context;
            _storageService = storageService;
            _objectStorage = objectStorage;
        }

        public Task<bool> IsOwnerAsync(Guid courseId, Guid userId, CancellationToken cancellationToken) =>
            _context.Courses.AnyAsync(c => c.Id == courseId && c.UserId == userId, cancellationToken);

        public async Task<IEnumerable<CourseDto>> ByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.Courses.AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Title)
                .Select(item => new CourseDto
                {
                    Id = item.Id,
                    Description = item.Description ?? "",
                    Title = item.Title,
                    State = item.State,
                    ImgUrl = item.ImgUrl
                }).ToListAsync(cancellationToken);
        }

        public async Task DeleteByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken)
        {
            var deleted = await _context.Courses.Where(c => c.UserId == userId && c.Id == courseId)
                 .ExecuteDeleteAsync(cancellationToken);

            if (deleted == 0) throw new NotFoundException(NotFound);

            await _storageService.DeleteCourseAsync(courseId);
        }

        public async Task UpdateImageByIdAsync(Guid courseId, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken)
        {
            var found = await _context.Courses.FirstOrDefaultAsync(c => c.UserId == userId && c.Id == courseId, cancellationToken)
                ?? throw new NotFoundException(NotFound);

            var key = await _storageService.SaveImageAsync(stream, fileName, courseId, cancellationToken);
            found.ImgUrl = _objectStorage.GetUrl(key);

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<CourseDto> GetByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken)
        {
            var result = await _context.Courses.AsNoTracking()
               .Where(c => c.UserId == userId && c.Id == courseId)
               .Select(item => new CourseDto
               {
                   Id = item.Id,
                   Description = item.Description ?? "",
                   Title = item.Title,
                   State = item.State,
                   ImgUrl = item.ImgUrl
               }).FirstOrDefaultAsync(cancellationToken);

            return result ?? throw new NotFoundException(NotFound);
        }

        public async Task<Guid> SaveAsync(string title, string description, bool state, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken)
        {
            var course = new CourseEntity { Title = title, Description = description, UserId = userId, State = state };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync(cancellationToken);

            var key = await _storageService.SaveImageAsync(stream, fileName, course.Id, cancellationToken);
            course.ImgUrl = _objectStorage.GetUrl(key);

            await _context.SaveChangesAsync(cancellationToken);

            return course.Id;
        }

        public async Task<IEnumerable<CourseDetailDto>> SearchAsync(string text, CancellationToken cancellationToken)
        {
            // Portal público: solo cursos activos. Sin texto devuelve todos (máx. 20).
            var terms = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var query = _context.Courses.AsNoTracking().Where(c => c.State);

            foreach (var term in terms)
            {
                query = query.Where(c => c.Title.Contains(term) || c.Description!.Contains(term));
            }

            return await query
               .OrderBy(c => c.Title)
               .Take(20)
               .Select(c => new CourseDetailDto
               {
                   Id = c.Id,
                   Title = c.Title,
                   Description = c.Description ?? "",
                   ImgUrl = c.ImgUrl
               })
              .ToListAsync(cancellationToken);
        }

        public async Task UpdateByIdAsync(Guid courseId, string title, string description, bool state, Guid userId, CancellationToken cancellationToken)
        {
            var updated = await _context.Courses
                .Where(c => c.Id == courseId && c.UserId == userId)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(c => c.Title, title)
                    .SetProperty(c => c.Description, description)
                    .SetProperty(c => c.State, state),
                    cancellationToken);

            if (updated == 0) throw new NotFoundException(NotFound);
        }

        public async Task<CourseStatsDto> StatsAsync(Guid userId, CancellationToken cancellationToken)
        {
            var courses = await _context.Courses.AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    c.Id, c.Title, c.Description, c.State, c.ImgUrl,
                    VideoCount = c.Videos.Count(),
                    ReadyCount = c.Videos.Count(v => v.Status == VideoStatus.Ready),
                    LastActivity = c.Videos.Max(v => (DateTime?)v.CreatedAt)
                })
                .ToListAsync(cancellationToken);

            var byStatus = await _context.Videos.AsNoTracking()
                .Where(v => v.Course.UserId == userId)
                .GroupBy(v => v.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-6);
            var perDay = await _context.Videos.AsNoTracking()
                .Where(v => v.Course.UserId == userId && v.CreatedAt >= from)
                .GroupBy(v => v.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            int Count(params string[] s) => byStatus.Where(x => s.Contains(x.Status)).Sum(x => x.Count);

            return new CourseStatsDto
            {
                Courses = courses.Count,
                Active = courses.Count(c => c.State),
                Inactive = courses.Count(c => !c.State),
                TotalVideos = byStatus.Sum(x => x.Count),
                Ready = Count(VideoStatus.Ready),
                Processing = Count(VideoStatus.Processing, VideoStatus.Uploading),
                Failed = Count(VideoStatus.Failed),
                Last7Days = Enumerable.Range(0, 7)
                    .Select(i => from.AddDays(i))
                    .Select(d => new DailyCountDto { Date = d, Count = perDay.FirstOrDefault(p => p.Day == d)?.Count ?? 0 })
                    .ToList(),
                RecentCourses = courses
                    .OrderByDescending(c => c.LastActivity ?? DateTime.MinValue)
                    .ThenBy(c => c.Title)
                    .Take(3)
                    .Select(c => new CourseSummaryDto
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Description = c.Description ?? "",
                        State = c.State,
                        ImgUrl = c.ImgUrl,
                        VideoCount = c.VideoCount,
                        ReadyCount = c.ReadyCount,
                        LastActivity = c.LastActivity
                    })
                    .ToList()
            };
        }
    }
}
