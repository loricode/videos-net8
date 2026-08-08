using DomainCore.DTOs.Course;
using DomainCore.Exceptions;
using InfrastructureCore.LocalStorage;
using InfrastructureCore.Persistence.EF;
using InfrastructureCore.Persistence.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureCore.Persistence.Course
{
    public class CourseRepository : ICourseRepository
    {

        private readonly AppDbContext _context;
        private readonly ILocalStorageService _storageService;
        public CourseRepository(AppDbContext context, ILocalStorageService storageServic)
        {
            _context = context;
            _storageService = storageServic;
        }

        public async Task<IEnumerable<CourseDto>> ByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {

            var result = await _context.Courses.Where(c => c.UserId == userId)
                .Select(item => new CourseDto 
                {
                   Id = item.Id,
                   Description = item.Description ?? "",
                   Title = item.Title,
                   State = item.State
                }).ToListAsync(cancellationToken);

            return result;

        }

        public async Task DeleteByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken)
        {
            var deleted = await _context.Courses.Where(c => c.UserId == userId && c.Id == courseId)
                 .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
            {
                await _storageService.DeleteCourseAsync(courseId);
            }
        }

        public async Task UpdateImageByIdAsync(Guid courseId, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken)
        {
            var found = await _context.Courses.Where(c => c.UserId == userId && c.Id == courseId)
                 .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("No existe el curso.");
            
            await _storageService.UpdateImageAsync(stream, fileName, courseId, cancellationToken);

            found.ImgUrl = $"/images/{found.Id}/{fileName.Trim()}";

            await _context.SaveChangesAsync(cancellationToken);

        }
        
        public async Task<CourseDto> GetByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken)
        {
            var result = await _context.Courses.Where(c => c.UserId == userId && c.Id == courseId)
               .AsNoTracking()
               .Select(item => new CourseDto
               {
                   Id = item.Id,
                   Description = item.Description ?? "",
                   Title = item.Title,
                   State = item.State
               }).FirstOrDefaultAsync(cancellationToken);

            return result ?? throw new NotFoundException("No existe el curso.");
        }

        public async Task<Guid> SaveAsync(string title, string description, bool state, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken)
        {
            
            var course = new CourseEntity { Title = title, Description = description, UserId = userId, State = state };

            _context.Courses.Add(course);

            await _context.SaveChangesAsync(cancellationToken);

            await _storageService.SaveImageAsync(stream, fileName, course.Id, cancellationToken);

            course.ImgUrl = $"/images/{course.Id}/{fileName.Trim()}";

            await _context.SaveChangesAsync(cancellationToken);

            return course.Id;

        }

        public async Task<IEnumerable<CourseDetailDto>> SearchAsync(string text, CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(text))
            {
                return Enumerable.Empty<CourseDetailDto>();
            }

            var terms = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var query = _context.Courses.AsNoTracking();

            foreach (var term in terms)
            {
                query = query.Where(c =>
                    c.Title.Contains(term) ||
                    c.Description!.Contains(term));
            }

            var result = await query
               .OrderByDescending(c => c.State)
               .ThenBy(c => c.Title)
               .Take(20)
               .Select(c => new CourseDetailDto
               {
                  Id = c.Id,
                  Title = c.Title,
                  Description = c.Description ?? "",
                  ImgUrl = c.ImgUrl
               })
              .ToListAsync(cancellationToken);

            return result;

        }

        public async Task UpdateByIdAsync(Guid courseId, string title, string description, bool state, Guid userId, CancellationToken cancellationToken)
        {
            await _context.Courses
                .Where(c => c.Id == courseId && c.UserId == userId)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(c => c.Title, title)
                    .SetProperty(c => c.Description, description)
                    .SetProperty(c => c.State, state),
                    cancellationToken);
        }
    }
}
