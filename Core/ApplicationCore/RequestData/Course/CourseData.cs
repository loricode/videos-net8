
namespace ApplicationCore.RequestData.Course
{
    public class CourseData
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Stream StreamFile { get; set; } = new MemoryStream();
        public string FileName { get; set; } = string.Empty;

    }
}
