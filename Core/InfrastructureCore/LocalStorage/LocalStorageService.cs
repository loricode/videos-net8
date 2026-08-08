
using InfrastructureCore.ProcessVideo;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Extensions.Msal;

namespace InfrastructureCore.LocalStorage
{
    public class LocalStorageService : ILocalStorageService
    {

        private readonly FfmepgOption _options;

        public LocalStorageService(IOptions<FfmepgOption> options)
        {
            _options = options.Value;
        }

        public async Task<FolderOption> SaveAsync(Stream stream, string fileName, Guid courseId, Guid videoId, CancellationToken cancellationToken)
        {

            var folder = Path.Combine(_options.PathVideos, "storage", courseId.ToString(), videoId.ToString());

            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, $"{fileName.Trim()}");
            
            await using var fileStream = File.Create(path);

            await stream.CopyToAsync(fileStream, cancellationToken);

            return new FolderOption { OriginalFilePath = path, OutputFolder = folder };
        }

        public async Task<string> SaveImageAsync(Stream stream, string fileName, Guid identification, CancellationToken cancellationToken)
        {

            var folder = Path.Combine(_options.PathVideos, "images", identification.ToString());

            Directory.CreateDirectory(folder);

            var path = Path.Combine(
                folder,
                $"{fileName}");

            await using var fileStream = File.Create(path);

            await stream.CopyToAsync(fileStream, cancellationToken);

            return path;
        }

        public async Task UpdateImageAsync(
    Stream stream,
    string fileName,
    Guid identification,
    CancellationToken cancellationToken)
        {
            var folder = Path.Combine(_options.PathVideos, "images", identification.ToString());

            Directory.CreateDirectory(folder);

            // Eliminar todas las imágenes anteriores
            foreach (var file in Directory.GetFiles(folder))
            {
                File.Delete(file);
            }

            var path = Path.Combine(folder, fileName);

            await using var fileStream = File.Create(path);
            await stream.CopyToAsync(fileStream, cancellationToken);

           
        }

        public Task DeleteCourseAsync(Guid courseId)
        {

            DeleteDirectory(Path.Combine(_options.PathVideos, "storage", courseId.ToString()));
            DeleteDirectory(Path.Combine(_options.PathVideos , "images", courseId.ToString()));

            return Task.CompletedTask;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

    }
}
