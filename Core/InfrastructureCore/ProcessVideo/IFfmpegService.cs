namespace InfrastructureCore.ProcessVideo
{
    public record MediaInfo(double DurationSeconds, int Height, bool HasAudio);

    public interface IFfmpegService
    {
        /// <summary>Duración, alto y si tiene pista de audio (ffprobe).</summary>
        Task<MediaInfo> ProbeAsync(string inputPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Genera HLS adaptativo (varias calidades) en outputFolder con playlist maestra
        /// "playlist.m3u8" y variantes en v0/, v1/…
        /// </summary>
        Task GenerateAbrHlsAsync(string inputPath, string outputFolder, MediaInfo info, CancellationToken cancellationToken = default);

        /// <summary>Extrae un fotograma como miniatura JPG (640 px de ancho).</summary>
        Task GenerateThumbnailAsync(string inputPath, string outputPath, double atSeconds, CancellationToken cancellationToken = default);
    }
}
