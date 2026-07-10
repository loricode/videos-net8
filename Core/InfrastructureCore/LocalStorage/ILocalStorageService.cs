
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureCore.LocalStorage
{
    public interface ILocalStorageService
    {
        Task<FolderOption> SaveAsync(Stream stream, string fileName, Guid courseId, Guid videoId, CancellationToken cancellationToken);
        Task<string> SaveImageAsync(Stream stream, string fileName, Guid identification, CancellationToken cancellationToken);

        Task DeleteCourseAsync(Guid courseId);

    }
}
