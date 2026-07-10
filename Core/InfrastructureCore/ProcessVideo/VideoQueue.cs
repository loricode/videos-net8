using System.Threading.Channels;

namespace InfrastructureCore.ProcessVideo
{
    public class VideoQueue : IVideoQueue
    {

        private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

        public ValueTask QueueAsync(Guid videoId)
        {
            return _queue.Writer.WriteAsync(videoId);
        }

        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }

    }
}
