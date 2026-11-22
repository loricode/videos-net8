using ApplicationCore.Interfaces.Course;
using DomainCore.Constants;
using DomainCore.DTOs.Course;
using InfrastructureCore.LocalStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApiCore.Extensions;

namespace WebApiCore.Controllers
{
    /// <summary>
    /// Gestión de cursos del instructor autenticado. El dueño SIEMPRE se toma del JWT;
    /// cualquier "userId" enviado por el cliente se ignora.
    /// </summary>
    [ApiController]
    [Authorize(Roles = Roles.Instructor)]
    [Route("[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseCreate _courseCreate;
        private readonly ICourseByUserId _courseByUserId;
        private readonly ICourseSearch _courseSearch;
        private readonly ICourseUpdate _courseUpdate;
        private readonly ICourseDelete _courseDelete;
        private readonly ICourseGet _courseGet;
        private readonly ICourseUpdateImage _courseUpdateImage;
        private readonly ICourseStats _courseStats;
        private readonly ILocalStorageService _files;

        public CourseController(ICourseCreate courseCreate, ICourseByUserId courseByUserId, ICourseSearch courseSearch,
            ICourseUpdate courseUpdate, ICourseDelete courseDelete, ICourseGet courseGet,
            ICourseUpdateImage courseUpdateImage, ICourseStats courseStats, ILocalStorageService files)
        {
            _courseCreate = courseCreate;
            _courseByUserId = courseByUserId;
            _courseSearch = courseSearch;
            _courseUpdate = courseUpdate;
            _courseDelete = courseDelete;
            _courseGet = courseGet;
            _courseUpdateImage = courseUpdateImage;
            _courseStats = courseStats;
            _files = files;
        }

        [HttpPost("Create")]
        [RequestSizeLimit(UploadLimits.ImageBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadLimits.ImageBytes)]
        public async Task<ActionResult<Guid>> Create([FromForm] string title, [FromForm] string? description, [FromForm] bool state,
            IFormFile file, CancellationToken cancellationToken)
        {
            if (_files.SafeExtension(file.FileName, LocalStorageService.ImageExtensions) is null)
                return BadRequest(new { message = "Formato de imagen no permitido." });

            await using var stream = file.OpenReadStream();
            return await _courseCreate.SaveAsync(title, description ?? string.Empty, state, User.GetUserId(),
                stream, file.FileName, cancellationToken);
        }

        [HttpPut("Update")]
        public Task Update([FromForm] Guid courseId, [FromForm] string title, [FromForm] string? description, [FromForm] bool state,
            CancellationToken cancellationToken)
        {
            return _courseUpdate.UpdateByIdAsync(courseId, title, description ?? string.Empty, state, User.GetUserId(), cancellationToken);
        }

        [HttpDelete("Delete")]
        public Task DeleteById([FromQuery] Guid courseId, CancellationToken cancellationToken)
        {
            return _courseDelete.DeleteByIdAsync(courseId, User.GetUserId(), cancellationToken);
        }

        [HttpGet("Get")]
        public Task<CourseDto> GetById([FromQuery] Guid courseId, CancellationToken cancellationToken)
        {
            return _courseGet.GetByIdAsync(courseId, User.GetUserId(), cancellationToken);
        }

        /// <summary>Cursos del instructor autenticado. (La variante con {userId} se mantiene por compatibilidad e ignora el parámetro.)</summary>
        [HttpGet("List")]
        [HttpGet("List/{ignoredUserId:guid}")]
        public Task<IEnumerable<CourseDto>> Mine(CancellationToken cancellationToken)
        {
            return _courseByUserId.ByUserIdAsync(User.GetUserId(), cancellationToken);
        }

        /// <summary>Resumen para el dashboard en una sola llamada.</summary>
        [HttpGet("Stats")]
        public Task<CourseStatsDto> Stats(CancellationToken cancellationToken)
        {
            return _courseStats.StatsAsync(User.GetUserId(), cancellationToken);
        }

        [HttpPut("UpdateImage")]
        [RequestSizeLimit(UploadLimits.ImageBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadLimits.ImageBytes)]
        public async Task<IActionResult> UpdateImage([FromForm] Guid courseId, IFormFile file, CancellationToken cancellationToken)
        {
            if (_files.SafeExtension(file.FileName, LocalStorageService.ImageExtensions) is null)
                return BadRequest(new { message = "Formato de imagen no permitido." });

            await using var stream = file.OpenReadStream();
            await _courseUpdateImage.UpdateImageByIdAsync(courseId, User.GetUserId(), stream, file.FileName, cancellationToken);
            return NoContent();
        }

        [HttpGet("Search")]
        [AllowAnonymous]
        public Task<IEnumerable<CourseDetailDto>> Search([FromQuery] string? parameter, CancellationToken cancellationToken)
        {
            return _courseSearch.SearchAsync(parameter ?? string.Empty, cancellationToken);
        }
    }
}
