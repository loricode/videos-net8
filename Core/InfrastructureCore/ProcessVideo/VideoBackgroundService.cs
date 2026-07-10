using InfrastructureCore.Persistence.EF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InfrastructureCore.ProcessVideo
{
    public class VideoBackgroundService : BackgroundService
    {

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IVideoQueue _videoQueue;
        private readonly FfmepgOption _options;

        public VideoBackgroundService(IServiceScopeFactory scopeFactory, IVideoQueue videoQueue, IOptions<FfmepgOption> options)
        {
            _scopeFactory = scopeFactory;
            _videoQueue = videoQueue;
            _options = options.Value;

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
           while(!stoppingToken.IsCancellationRequested) {

                var videoId = await _videoQueue.DequeueAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();

                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var ffmpeg = scope.ServiceProvider.GetRequiredService<IFfmpegService>();

                var video = await context.Videos.FindAsync([videoId], stoppingToken);

                if (video is null)
                {
                    continue;
                }

                try
                {
                    if (video != null)
                    {

                        var outputFolder = Path.Combine(_options.PathVideos, "storage", video.CourseId.ToString(), video.Id.ToString());

                        await ffmpeg.GenerateHslAsync(video.FilePath, outputFolder, stoppingToken);

                        video.StreamUrl = $"/storage/{video.CourseId}/{video.Id}/playlist.m3u8";

                        video.Status = "Ready";

                        await context.SaveChangesAsync(stoppingToken);

                    }
                    

                }
                catch
                {
                    video.Status = "Failed";

                    await context.SaveChangesAsync(stoppingToken);
                }

            }
        }
    }
}

