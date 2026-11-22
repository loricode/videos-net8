namespace DomainCore.DTOs.Progress
{
    public class ProgressDto
    {
        public Guid VideoId { get; set; }
        public double PositionSeconds { get; set; }
        public bool Completed { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ProgressSaveData
    {
        public Guid VideoId { get; set; }
        public double PositionSeconds { get; set; }
        public bool Completed { get; set; }
    }
}
