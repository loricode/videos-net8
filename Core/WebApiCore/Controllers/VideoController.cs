using ApplicationCore.Interfaces.Video;
using DomainCore.Constants;
using DomainCore.DTOs.Video;
using InfrastructureCore.LocalStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApiCore.Extensions;

namespace WebApiCore.Controllers
{
    [ApiController]
    [Authorize(Roles = Roles.Instructor)]
    [Route("[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IUploadVideo _uploadVideo;
        private readonly IVideoByCourseId _videoByCourseId;
        private readonly IVideosFromCourseId _videosFromCourseId;
        private readonly IVideoDelete _videoDelete;
        private readonly IVideoReprocess _videoReprocess;
        private readonly ILocalStorageService _files;

        public VideoController(IUploadVideo uploadVideo, IVideoByCourseId videoByCourseId,
            IVideosFromCourseId videosFromCourseId, IVideoDelete videoDelete,
            IVideoReprocess videoReprocess, ILocalStorageService files)
        {
            _uploadVideo = uploadVideo;
            _videoByCourseId = videoByCourseId;
            _videosFromCourseId = videosFromCourseId;
            _videoDelete = videoDelete;
            _videoReprocess = videoReprocess;
            _files = files;
        }

        [HttpPost("Upload")]
        [RequestSizeLimit(UploadLimits.VideoBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadLimits.VideoBytes)]
        public async Task<ActionResult<Guid>> Upload([FromForm] string title, [FromForm] string? description,
            IFormFile file, [FromForm] Guid courseId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest(new { message = "El título es obligatorio." });

            if (_files.SafeExtension(file.FileName, LocalStorageService.VideoExtensions) is null)
                return BadRequest(new { message = "Formato de video no permitido." });

            await using var stream = file.OpenReadStream();
            return await _uploadVideo.UploadAsync(stream, file.FileName, title, description ?? string.Empty,
                courseId, User.GetUserId(), cancellationToken);
        }

        [HttpGet("List/{courseId:guid}")]
        public Task<IEnumerable<VideoDto>> ByCourseId(Guid courseId, CancellationToken cancellationToken)
        {
            return _videoByCourseId.ByCourseIdAsync(courseId, User.GetUserId(), cancellationToken);
        }

        [HttpGet("ListByCourseId")]
        [AllowAnonymous]
        public Task<VideoDetailDto> ListByCourseId([FromQuery] Guid parameter, CancellationToken cancellationToken)
        {
            return _videosFromCourseId.FromCourseIdAsync(parameter, cancellationToken);
        }

        [HttpDelete("Delete")]
        public Task DeleteById([FromQuery] Guid courseId, [FromQuery] Guid videoId, CancellationToken cancellationToken)
        {
            return _videoDelete.DeleteByIdAsync(courseId, videoId, User.GetUserId(), cancellationToken);
        }

        /// <summary>Reintenta el procesamiento de una lección con error.</summary>
        [HttpPost("Reprocess")]
        public async Task<IActionResult> Reprocess([FromQuery] Guid videoId, CancellationToken cancellationToken)
        {
            await _videoReprocess.ReprocessAsync(videoId, User.GetUserId(), cancellationToken);
            return Accepted();
        }
    }
}
