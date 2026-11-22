using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace InfrastructureCore.ProcessVideo
{
    public class FfmpegService : IFfmpegService
    {
        /// <summary>Escalera de calidades: alto, bitrate de video, bitrate máximo, buffer.</summary>
        private static readonly (int Height, string Bitrate, string MaxRate, string BufSize)[] Ladder =
        [
            (360,  "800k",  "856k",  "1200k"),
            (720,  "2800k", "2996k", "4200k"),
            (1080, "5000k", "5350k", "7500k"),
        ];

        private readonly FfmepgOption _options;

        public FfmpegService(IOptions<FfmepgOption> options)
        {
            _options = options.Value;
        }

        private string ProbePath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_options.PathProbe)) return _options.PathProbe;
                var dir = Path.GetDirectoryName(_options.PathOrigen) ?? string.Empty;
                var exe = OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";
                return Path.Combine(dir, exe);
            }
        }

        public async Task<MediaInfo> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
        {
            var args = new[] { "-v", "error", "-print_format", "json", "-show_format", "-show_streams", inputPath };
            var (code, stdout, stderr) = await RunAsync(ProbePath, args, null, cancellationToken);
            if (code != 0) throw new InvalidOperationException($"ffprobe falló: {stderr}");

            using var doc = JsonDocument.Parse(stdout);
            var root = doc.RootElement;

            double duration = 0;
            if (root.TryGetProperty("format", out var format) && format.TryGetProperty("duration", out var d))
                double.TryParse(d.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out duration);

            int height = 0;
            bool hasAudio = false;
            foreach (var s in root.GetProperty("streams").EnumerateArray())
            {
                var type = s.GetProperty("codec_type").GetString();
                if (type == "video" && height == 0 && s.TryGetProperty("height", out var h)) height = h.GetInt32();
                if (type == "audio") hasAudio = true;
            }

            return new MediaInfo(duration, height, hasAudio);
        }

        public async Task GenerateAbrHlsAsync(string inputPath, string outputFolder, MediaInfo info, CancellationToken cancellationToken = default)
        {
            // Variantes que no superen la resolución de origen (al menos 360p)
            var variants = Ladder.Where(l => l.Height <= Math.Max(info.Height, 360)).ToList();
            var n = variants.Count;

            var filter = new StringBuilder($"[0:v]split={n}");
            for (var i = 0; i < n; i++) filter.Append($"[v{i}]");
            filter.Append(';');
            for (var i = 0; i < n; i++)
                filter.Append($"[v{i}]scale=-2:{variants[i].Height}[v{i}o]{(i < n - 1 ? ";" : "")}");

            var args = new List<string> { "-y", "-i", inputPath, "-filter_complex", filter.ToString() };

            for (var i = 0; i < n; i++)
            {
                args.AddRange(["-map", $"[v{i}o]"]);
                args.AddRange([$"-c:v:{i}", "libx264", $"-b:v:{i}", variants[i].Bitrate,
                    $"-maxrate:v:{i}", variants[i].MaxRate, $"-bufsize:v:{i}", variants[i].BufSize]);
            }
            if (info.HasAudio)
            {
                for (var i = 0; i < n; i++) args.AddRange(["-map", "0:a:0"]);
                args.AddRange(["-c:a", "aac", "-b:a", "128k", "-ac", "2"]);
            }

            var streamMap = string.Join(' ', Enumerable.Range(0, n).Select(i => info.HasAudio ? $"v:{i},a:{i}" : $"v:{i}"));

            args.AddRange([
                "-preset", "veryfast", "-profile:v", "main", "-pix_fmt", "yuv420p",
                "-g", "48", "-keyint_min", "48", "-sc_threshold", "0",
                "-f", "hls", "-hls_time", "6", "-hls_playlist_type", "vod",
                "-hls_segment_filename", "v%v/segment_%03d.ts",
                "-master_pl_name", "playlist.m3u8",
                "-var_stream_map", streamMap,
                "v%v/index.m3u8"
            ]);

            for (var i = 0; i < n; i++) Directory.CreateDirectory(Path.Combine(outputFolder, $"v{i}"));

            var (code, _, stderr) = await RunAsync(_options.PathOrigen, args, outputFolder, cancellationToken);
            if (code != 0) throw new InvalidOperationException($"FFmpeg falló: {Tail(stderr)}");
        }

        public async Task GenerateThumbnailAsync(string inputPath, string outputPath, double atSeconds, CancellationToken cancellationToken = default)
        {
            var args = new[]
            {
                "-y", "-ss", atSeconds.ToString("0.###", CultureInfo.InvariantCulture), "-i", inputPath,
                "-frames:v", "1", "-vf", "scale=640:-2", "-q:v", "4", outputPath
            };
            var (code, _, stderr) = await RunAsync(_options.PathOrigen, args, null, cancellationToken);
            if (code != 0) throw new InvalidOperationException($"No se pudo generar la miniatura: {Tail(stderr)}");
        }

        private static async Task<(int Code, string StdOut, string StdErr)> RunAsync(
            string fileName, IEnumerable<string> args, string? workingDirectory, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            if (workingDirectory != null) psi.WorkingDirectory = workingDirectory;
            // ArgumentList evita problemas de comillas e inyección en rutas
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ya terminó */ }
                throw;
            }

            return (process.ExitCode, await stdoutTask, await stderrTask);
        }

        private static string Tail(string s) => s.Length > 1500 ? s[^1500..] : s;
    }
}
