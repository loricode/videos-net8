
namespace ApplicationCore.Interfaces.Video
{
    public interface IVideoDelete
    {
        Task DeleteByIdAsync(Guid courseId, Guid videoId, Guid userId, CancellationToken cancellationToken);

    }
}
