using ApplicationCore.Interfaces.Video;
using DomainCore.DTOs.Course;
using DomainCore.DTOs.Video;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApiCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoController: ControllerBase
    {

        private readonly IUploadVideo _uploadVideo;

        private readonly IVideoByCourseId _videoByCourseId;

        private readonly IVideosFromCourseId _videosFromCourseId;

        public VideoController(IUploadVideo uploadVideo, IVideoByCourseId videoByCourseId, IVideosFromCourseId videosFromCourseId) 
        {
           _uploadVideo = uploadVideo;
           _videoByCourseId = videoByCourseId;
           _videosFromCourseId = videosFromCourseId;
        }

        [HttpPost("Upload")]
        public async Task<Guid> Upload([FromForm] string title, [FromForm] string description,
        [FromForm] IFormFile file, [FromForm] Guid courseId, CancellationToken cancellationToken)
        {
            return await _uploadVideo.UploadAsync(file.OpenReadStream(), file.FileName, title, description, courseId, cancellationToken);
        }

        [HttpGet("List/{courseId:guid}")]
        public async Task<IEnumerable<VideoDto>> ByCourseId(Guid courseId, CancellationToken cancellationToken)
        {
            return await _videoByCourseId.ByCourseIdAsync(courseId, cancellationToken);
        }

        [HttpGet("ListByCourseId")]
        [AllowAnonymous]
        public async Task<VideoDetailDto> ListByCourseId([FromQuery] Guid parameter, CancellationToken cancellationToken)
        {
            return await _videosFromCourseId.FromCourseIdAsync(parameter, cancellationToken);
        }

    }
}
