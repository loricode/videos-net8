
namespace DomainCore.DTOs.Video
{
    public class VideoDetailDto
    {
        public string TitleCourse {  get; set; } = string.Empty;

        public List<VideoItem> Items { get; set; } = new ();

    }

    public class VideoItem {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;
    }

}
