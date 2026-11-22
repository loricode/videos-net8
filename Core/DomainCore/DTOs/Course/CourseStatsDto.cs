namespace DomainCore.DTOs.Course
{
    public class CourseStatsDto
    {
        public int Courses { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
        public int TotalVideos { get; set; }
        public int Ready { get; set; }
        public int Processing { get; set; }
        public int Failed { get; set; }
        /// <summary>Videos subidos por día (UTC) en los últimos 7 días, del más antiguo a hoy.</summary>
        public List<DailyCountDto> Last7Days { get; set; } = new();
        public List<CourseSummaryDto> RecentCourses { get; set; } = new();
    }

    public class DailyCountDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class CourseSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool State { get; set; }
        public string? ImgUrl { get; set; }
        public int VideoCount { get; set; }
        public int ReadyCount { get; set; }
        public DateTime? LastActivity { get; set; }
    }
}
