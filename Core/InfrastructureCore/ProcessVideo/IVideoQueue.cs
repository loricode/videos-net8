using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureCore.ProcessVideo
{
    public interface IVideoQueue
    {
        ValueTask QueueAsync(Guid videoId);
        ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);

    }
}
