using System;

namespace ApplicationCore.Interfaces.Course
{
    public interface ICourseDelete
    {
        Task DeleteByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken);

    }
}
