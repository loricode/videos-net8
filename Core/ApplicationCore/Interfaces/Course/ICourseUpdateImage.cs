
namespace ApplicationCore.Interfaces.Course
{
    public interface ICourseUpdateImage
    {
        Task UpdateImageByIdAsync(Guid courseId, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken);

    }
}
