using InfrastructureCore.ObjectStorage;
using InfrastructureCore.ProcessVideo;
using Microsoft.Extensions.Options;

namespace InfrastructureCore.LocalStorage
{
    public class LocalStorageService : ILocalStorageService
    {
        public static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
        public static readonly string[] VideoExtensions = [".mp4", ".webm", ".mov", ".avi", ".mkv"];

        private readonly FfmepgOption _options;
        private readonly IObjectStorage _objectStorage;

        public LocalStorageService(IOptions<FfmepgOption> options, IObjectStorage objectStorage)
        {
            _options = options.Value;
            _objectStorage = objectStorage;
        }

        public string? SafeExtension(string fileName, IEnumerable<string> allowed)
        {
            var ext = Path.GetExtension(Path.GetFileName(fileName ?? string.Empty)).ToLowerInvariant();
            return allowed.Contains(ext) ? ext : null;
        }

        public async Task<FolderOption> SaveAsync(Stream stream, string fileName, Guid courseId, Guid videoId, CancellationToken cancellationToken)
        {
            var ext = SafeExtension(fileName, VideoExtensions) ?? ".mp4";
            var folder = Path.Combine(_options.PathVideos, "storage", courseId.ToString(), videoId.ToString());
            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, "source" + ext);
            await using (var fileStream = File.Create(path))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            return new FolderOption { OriginalFilePath = path, OutputFolder = folder };
        }

        public async Task<string> SaveImageAsync(Stream stream, string fileName, Guid courseId, CancellationToken cancellationToken)
        {
            var ext = SafeExtension(fileName, ImageExtensions) ?? ".jpg";
            var prefix = $"images/{courseId}";

            // reemplazar portadas anteriores (local y bucket)
            await _objectStorage.DeletePrefixAsync(prefix, cancellationToken);

            var folder = Path.Combine(_options.PathVideos, "images", courseId.ToString());
            Directory.CreateDirectory(folder);

            // nombre con marca de tiempo: evita caché del navegador/CDN al cambiar portada
            var name = $"cover-{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            var path = Path.Combine(folder, name);
            await using (var fileStream = File.Create(path))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            var key = $"{prefix}/{name}";
            await _objectStorage.PublishFileAsync(path, key, cancellationToken);
            return key;
        }

        public async Task DeleteCourseAsync(Guid courseId)
        {
            await _objectStorage.DeletePrefixAsync($"storage/{courseId}");
            await _objectStorage.DeletePrefixAsync($"images/{courseId}");
        }

        public Task DeleteVideoAsync(Guid courseId, Guid videoId)
            => _objectStorage.DeletePrefixAsync($"storage/{courseId}/{videoId}");
    }
}
