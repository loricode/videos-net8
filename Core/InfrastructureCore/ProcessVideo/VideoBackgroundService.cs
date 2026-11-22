using DomainCore.Constants;
using InfrastructureCore.ObjectStorage;
using InfrastructureCore.Persistence.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InfrastructureCore.ProcessVideo
{
    /// <summary>
    /// Procesa la cola de videos: ffprobe → miniatura → HLS multi-calidad → publicación.
    /// Al iniciar re-encola los videos que quedaron pendientes (la BD es la fuente persistente
    /// de la cola; el Channel en memoria es solo el disparador).
    /// </summary>
    public class VideoBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IVideoQueue _videoQueue;
        private readonly ILogger<VideoBackgroundService> _logger;

        public VideoBackgroundService(IServiceScopeFactory scopeFactory, IVideoQueue videoQueue, ILogger<VideoBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _videoQueue = videoQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RecoverPendingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                Guid videoId;
                try
                {
                    videoId = await _videoQueue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await ProcessAsync(videoId, stoppingToken);
            }
        }

        private async Task RecoverPendingAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Esperar a que exista el esquema (las migraciones corren al iniciar la API)
                for (var i = 0; i < 30 && !ct.IsCancellationRequested; i++)
                {
                    try { await context.Videos.AnyAsync(ct); break; }
                    catch (Exception) when (!ct.IsCancellationRequested) { await Task.Delay(TimeSpan.FromSeconds(5), ct); }
                }

                var pending = await context.Videos
                    .Where(v => v.Status == VideoStatus.Uploading || v.Status == VideoStatus.Processing)
                    .Select(v => v.Id)
                    .ToListAsync(ct);

                foreach (var id in pending) await _videoQueue.QueueAsync(id);

                if (pending.Count > 0)
                    _logger.LogInformation("Re-encolados {Count} videos pendientes.", pending.Count);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo recuperar la cola de videos.");
            }
        }

        private async Task ProcessAsync(Guid videoId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ffmpeg = scope.ServiceProvider.GetRequiredService<IFfmpegService>();
            var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();

            var video = await context.Videos.FindAsync([videoId], ct);
            if (video is null || video.Status == VideoStatus.Ready) return;

            if (string.IsNullOrEmpty(video.FilePath) || !File.Exists(video.FilePath))
            {
                video.Status = VideoStatus.Failed;
                await context.SaveChangesAsync(ct);
                _logger.LogWarning("Video {Id} sin archivo original; marcado como Failed.", videoId);
                return;
            }

            try
            {
                video.Status = VideoStatus.Processing;
                await context.SaveChangesAsync(ct);

                var outputFolder = Path.GetDirectoryName(video.FilePath)!;
                var prefix = $"storage/{video.CourseId}/{video.Id}";

                var info = await ffmpeg.ProbeAsync(video.FilePath, ct);

                var thumbPath = Path.Combine(outputFolder, "thumb.jpg");
                var thumbAt = Math.Clamp(info.DurationSeconds * 0.1, 0, 5);
                try { await ffmpeg.GenerateThumbnailAsync(video.FilePath, thumbPath, thumbAt, ct); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning("Miniatura no generada para {Id}: {Msg}", videoId, ex.Message);
                }

                await ffmpeg.GenerateAbrHlsAsync(video.FilePath, outputFolder, info, ct);

                // El original ya no es necesario una vez generado el HLS
                File.Delete(video.FilePath);
                video.FilePath = string.Empty;

                var hasThumb = File.Exists(thumbPath);
                await storage.PublishDirectoryAsync(outputFolder, prefix, ct);

                video.StreamUrl = storage.GetUrl($"{prefix}/playlist.m3u8");
                video.ThumbnailUrl = hasThumb ? storage.GetUrl($"{prefix}/thumb.jpg") : null;
                video.DurationSeconds = Math.Round(info.DurationSeconds, 2);
                video.Status = VideoStatus.Ready;
                await context.SaveChangesAsync(ct);

                _logger.LogInformation("Video {Id} listo ({Duration:0.#}s, {Height}p).", videoId, info.DurationSeconds, info.Height);
            }
            catch (OperationCanceledException)
            {
                // Apagado: queda en Processing y se re-encola al próximo arranque
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando video {Id}", videoId);
                video.Status = VideoStatus.Failed;
                await context.SaveChangesAsync(CancellationToken.None);
            }
        }
    }
}
