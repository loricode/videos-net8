using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;

namespace InfrastructureCore.ProcessVideo
{
    public interface IFfmpegService
    {

        Task GenerateHslAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);

    }
}
