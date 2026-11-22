using DomainCore.Constants;
using DomainCore.DTOs.Progress;
using DomainCore.Exceptions;
using InfrastructureCore.Persistence.EF;
using InfrastructureCore.Persistence.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureCore.Persistence.Progress
{
    public interface IProgressRepository
    {
        Task<IEnumerable<ProgressDto>> ByCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken);
        Task SaveAsync(Guid userId, ProgressSaveData data, CancellationToken cancellationToken);
    }

    public class ProgressRepository : IProgressRepository
    {
        private readonly AppDbContext _context;

        public ProgressRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProgressDto>> ByCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
        {
            return await _context.VideoProgress.AsNoTracking()
                .Where(p => p.UserId == userId && p.Video.CourseId == courseId)
                .Select(p => new ProgressDto
                {
                    VideoId = p.VideoId,
                    PositionSeconds = p.PositionSeconds,
                    Completed = p.Completed,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task SaveAsync(Guid userId, ProgressSaveData data, CancellationToken cancellationToken)
        {
            // Solo lecciones públicas (listas y de cursos activos)
            var valid = await _context.Videos.AnyAsync(v => v.Id == data.VideoId && v.Status == VideoStatus.Ready && v.Course.State, cancellationToken);
            if (!valid) throw new NotFoundException("La lección no existe.");

            var entity = await _context.VideoProgress.FindAsync([userId, data.VideoId], cancellationToken);
            if (entity is null)
            {
                entity = new VideoProgressEntity { UserId = userId, VideoId = data.VideoId };
                _context.VideoProgress.Add(entity);
            }

            entity.PositionSeconds = Math.Max(0, data.PositionSeconds);
            // una lección completada no vuelve a "no completada"
            entity.Completed = entity.Completed || data.Completed;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
