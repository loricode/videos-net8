using ApplicationCore.Interfaces.Progress;
using DomainCore.DTOs.Progress;
using InfrastructureCore.Persistence.Progress;

namespace ApplicationCore.Querys.Progress
{
    public class ProgressByCourse : IProgressByCourse
    {
        private readonly IProgressRepository _repository;

        public ProgressByCourse(IProgressRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<ProgressDto>> ByCourseAsync(Guid userId, Guid courseId, CancellationToken cancellationToken)
            => _repository.ByCourseAsync(userId, courseId, cancellationToken);
    }
}
