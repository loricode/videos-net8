using ApplicationCore.Interfaces.Progress;
using DomainCore.DTOs.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApiCore.Extensions;

namespace WebApiCore.Controllers
{
    /// <summary>Progreso del usuario autenticado (estudiante o instructor) en las lecciones.</summary>
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressByCourse _byCourse;
        private readonly IProgressSave _save;

        public ProgressController(IProgressByCourse byCourse, IProgressSave save)
        {
            _byCourse = byCourse;
            _save = save;
        }

        [HttpGet("Course/{courseId:guid}")]
        public Task<IEnumerable<ProgressDto>> ByCourse(Guid courseId, CancellationToken cancellationToken)
            => _byCourse.ByCourseAsync(User.GetUserId(), courseId, cancellationToken);

        [HttpPut]
        public async Task<IActionResult> Save(ProgressSaveData data, CancellationToken cancellationToken)
        {
            await _save.SaveAsync(User.GetUserId(), data, cancellationToken);
            return NoContent();
        }
    }
}
