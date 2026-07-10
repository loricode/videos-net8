using ApplicationCore.Interfaces.Course;
using DomainCore.DTOs.Course;
using InfrastructureCore.Persistence.Course;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Querys.Course
{
    public class CourseSearch : ICourseSearch
    {

        private readonly ICourseRepository _courseRepository;

        public CourseSearch(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }
        public async Task<IEnumerable<CourseDetailDto>> SearchAsync(string text, CancellationToken cancellationToken)
        {
            return await _courseRepository.SearchAsync(text, cancellationToken);
        }
    }
}
