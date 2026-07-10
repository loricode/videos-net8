using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces.Course
{
    public interface ICourseUpdate
    {
        Task UpdateByIdAsync(Guid courseId, string title, string description, bool state, Guid userId, CancellationToken cancellationToken);

    }
}
