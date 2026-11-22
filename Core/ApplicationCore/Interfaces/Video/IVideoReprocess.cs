namespace ApplicationCore.Interfaces.Video
{
    public interface IVideoReprocess
    {
        Task ReprocessAsync(Guid videoId, Guid userId, CancellationToken cancellationToken);
    }
}
