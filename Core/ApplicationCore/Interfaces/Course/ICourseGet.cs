using DomainCore.DTOs.Course;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces.Course
{
    public interface ICourseGet
    {
        Task<CourseDto> GetByIdAsync(Guid courseId, Guid userId, CancellationToken cancellationToken);

    }
}
