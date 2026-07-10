
using ApplicationCore.RequestData.Course;

namespace ApplicationCore.Interfaces.Course
{
    public interface ICourseCreate
    {
        Task<Guid> SaveAsync(string title, string description, bool state, Guid userId, Stream stream, string fileName, CancellationToken cancellationToken);

    }
}
