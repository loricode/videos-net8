namespace InfrastructureCore.ObjectStorage
{
    /// <summary>
    /// Publicación de archivos de medios (HLS, miniaturas, portadas).
    /// Las claves tienen la forma "storage/{curso}/{video}/..." o "images/{curso}/...",
    /// relativas a la carpeta de trabajo local (FFmpeg:PathVideos).
    /// </summary>
    public interface IObjectStorage
    {
        /// <summary>Publica todos los archivos de una carpeta local bajo el prefijo dado.</summary>
        Task PublishDirectoryAsync(string localDirectory, string keyPrefix, CancellationToken cancellationToken = default);

        /// <summary>Publica un archivo local con la clave dada.</summary>
        Task PublishFileAsync(string localPath, string key, CancellationToken cancellationToken = default);

        /// <summary>Elimina todo lo publicado bajo el prefijo (y su copia local).</summary>
        Task DeletePrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);

        /// <summary>URL pública de la clave: relativa ("/storage/...") en Local, absoluta (CDN/S3) en S3.</summary>
        string GetUrl(string key);
    }

    public class ObjectStorageOptions
    {
        /// <summary>"Local" (por defecto) o "S3".</summary>
        public string Provider { get; set; } = "Local";
        public string ServiceUrl { get; set; } = string.Empty;
        public string Region { get; set; } = "us-east-1";
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Bucket { get; set; } = "videos";
        /// <summary>Base pública (CDN o endpoint del bucket), p. ej. https://cdn.midominio.com</summary>
        public string PublicBaseUrl { get; set; } = string.Empty;
        /// <summary>Crear el bucket con lectura anónima si no existe (útil con MinIO).</summary>
        public bool EnsureBucket { get; set; }
        /// <summary>Borrar los archivos locales tras subirlos.</summary>
        public bool DeleteLocalAfterPublish { get; set; } = true;
    }

    internal static class MediaContentTypes
    {
        public static string For(string path) => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".m3u8" => "application/vnd.apple.mpegurl",
            ".ts" => "video/mp2t",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
