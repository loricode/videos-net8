using ApplicationCore.Interfaces.Course;
using DomainCore.DTOs.Course;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApiCore.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class CourseController: ControllerBase
    {
        private readonly ICourseCreate _courseCreate;

        private readonly ICourseByUserId _courseByUserId;

        private readonly ICourseSearch _courseSearch;

        private readonly ICourseUpdate _courseUpdate;

        private readonly ICourseDelete _courseDelete;

        private readonly ICourseGet _courseGet;
        public CourseController(ICourseCreate courseCreate, ICourseByUserId courseByUserId, ICourseSearch 
            courseSearch, ICourseUpdate courseUpdate, ICourseDelete courseDelete, ICourseGet courseGet) 
        { 
            _courseCreate = courseCreate;
            _courseByUserId = courseByUserId;
            _courseSearch = courseSearch;
            _courseUpdate = courseUpdate;
            _courseDelete = courseDelete;
            _courseGet = courseGet; 
        }

        [HttpPost("Create")]
        public async Task<Guid> Create([FromForm] string title, [FromForm] string description, [FromForm] bool state,
        [FromForm] IFormFile file, [FromForm] Guid userId, CancellationToken cancellationToken)
        {
            return await _courseCreate.SaveAsync(title, description, state, userId, file.OpenReadStream(),
                file.FileName, cancellationToken);
        }

        [HttpPut("Update")]
        public Task Update([FromForm] Guid courseId, [FromForm] string title, [FromForm] string description, [FromForm] bool state,
         [FromForm] Guid userId, CancellationToken cancellationToken)
        {
            return _courseUpdate.UpdateByIdAsync(courseId, title, description,
                state, userId, cancellationToken);
        }

        [HttpDelete("Delete")]
        public Task DeleteById([FromQuery] Guid courseId, [FromQuery] Guid userId, CancellationToken cancellationToken)
        {
            return _courseDelete.DeleteByIdAsync(courseId, userId, cancellationToken);
        }

        [HttpGet("Get")]
        public async Task<CourseDto> ByUserId([FromQuery] Guid courseId, [FromQuery] Guid userId, CancellationToken cancellationToken)
        {
            return await _courseGet.GetByIdAsync(courseId, userId, cancellationToken);
        }

        [HttpGet("List/{userId:guid}")]
        public async Task<IEnumerable<CourseDto>> ByUserId(Guid userId, CancellationToken cancellationToken)
        {
            return await _courseByUserId.ByUserIdAsync(userId, cancellationToken);
        }

        [HttpGet("Search")]
        [AllowAnonymous]
        public async Task<IEnumerable<CourseDetailDto>> Search([FromQuery] string parameter, CancellationToken cancellationToken)
        {
            return await _courseSearch.SearchAsync(parameter, cancellationToken);
        }

    }
}