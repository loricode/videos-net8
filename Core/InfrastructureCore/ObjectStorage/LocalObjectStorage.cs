using InfrastructureCore.ProcessVideo;
using Microsoft.Extensions.Options;

namespace InfrastructureCore.ObjectStorage
{
    /// <summary>
    /// Proveedor local: los archivos ya están en la carpeta de trabajo y la API los sirve
    /// como estáticos en /storage y /images. Publicar es un no-op.
    /// </summary>
    public class LocalObjectStorage : IObjectStorage
    {
        private readonly string _root;

        public LocalObjectStorage(IOptions<FfmepgOption> options)
        {
            _root = options.Value.PathVideos;
        }

        public Task PublishDirectoryAsync(string localDirectory, string keyPrefix, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PublishFileAsync(string localPath, string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeletePrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
        {
            var path = Path.GetFullPath(Path.Combine(_root, keyPrefix));
            var root = Path.GetFullPath(_root);

            // Nunca borrar fuera de la carpeta raíz
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || path.TrimEnd('/', '\\') == root.TrimEnd('/', '\\'))
                return Task.CompletedTask;

            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            else if (File.Exists(path)) File.Delete(path);

            return Task.CompletedTask;
        }

        public string GetUrl(string key) => "/" + key.Replace('\\', '/').TrimStart('/');
    }
}
