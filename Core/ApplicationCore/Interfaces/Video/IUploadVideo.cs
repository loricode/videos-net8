
namespace ApplicationCore.Interfaces.Video
{
    public interface IUploadVideo
    {
        Task<Guid> UploadAsync(Stream stream, string fileName, string title, string description, Guid courseId, CancellationToken cancellationToken);
    }
}
