using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace InfrastructureCore.ProcessVideo
{
    public class FfmpegService: IFfmpegService
    {
        private readonly FfmepgOption _options;

        public FfmpegService(IOptions<FfmepgOption> options)
        {
            _options = options.Value;
        }

        public async Task GenerateHslAsync(string inputPath, string outputFolder, CancellationToken cancellationToken = default)
        {
            
            var arguments = $"-i \"{inputPath}\" " + "-hls_time 6 " + "-hls_playlist_type vod " + "-hls_segment_filename \"segment_%03d.ts\" " + "\"playlist.m3u8\"";
       
            await ExecuteAsync(arguments, outputFolder, cancellationToken);
        }

        private async Task ExecuteAsync(string arguments, string outputFolder, CancellationToken cancellationToken)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _options.PathOrigen,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = outputFolder,
                }
            };

            process.Start();

            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0) { throw new InvalidOperationException($"FFmpeg fallo:{error}"); }
            
        }
    } 
}