namespace InfrastructureCore.Persistence.EF.Entities
{
    /// <summary>Progreso de un estudiante en una lección.</summary>
    public class VideoProgressEntity
    {
        public Guid UserId { get; set; }
        public Guid VideoId { get; set; }
        public double PositionSeconds { get; set; }
        public bool Completed { get; set; }
        public DateTime UpdatedAt { get; set; }
        public VideoEntity Video { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}
