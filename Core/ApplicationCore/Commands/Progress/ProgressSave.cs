using ApplicationCore.Interfaces.Progress;
using DomainCore.DTOs.Progress;
using InfrastructureCore.Persistence.Progress;

namespace ApplicationCore.Commands.Progress
{
    public class ProgressSave : IProgressSave
    {
        private readonly IProgressRepository _repository;

        public ProgressSave(IProgressRepository repository)
        {
            _repository = repository;
        }

        public Task SaveAsync(Guid userId, ProgressSaveData data, CancellationToken cancellationToken)
            => _repository.SaveAsync(userId, data, cancellationToken);
    }
}
