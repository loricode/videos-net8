using DomainCore.DTOs.Progress;

namespace ApplicationCore.Interfaces.Progress
{
    public interface IProgressByCourse
    {
        Task<IEnumerable<ProgressDto>> ByCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken);
    }

    public interface IProgressSave
    {
        Task SaveAsync(Guid userId, ProgressSaveData data, CancellationToken cancellationToken);
    }
}
