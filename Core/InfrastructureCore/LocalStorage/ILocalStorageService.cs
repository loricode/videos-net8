namespace InfrastructureCore.LocalStorage
{
    /// <summary>Archivos de trabajo locales (originales y portadas) con nombres seguros.</summary>
    public interface ILocalStorageService
    {
        /// <summary>Guarda el video original como storage/{curso}/{video}/source.ext</summary>
        Task<FolderOption> SaveAsync(Stream stream, string fileName, Guid courseId, Guid videoId, CancellationToken cancellationToken);

        /// <summary>Reemplaza la portada del curso y devuelve su clave ("images/{curso}/cover-xxx.ext").</summary>
        Task<string> SaveImageAsync(Stream stream, string fileName, Guid courseId, CancellationToken cancellationToken);

        /// <summary>Elimina portadas, videos y HLS del curso.</summary>
        Task DeleteCourseAsync(Guid courseId);

        /// <summary>Elimina la carpeta del video (original + HLS + miniatura).</summary>
        Task DeleteVideoAsync(Guid courseId, Guid videoId);

        /// <summary>Extensión permitida en minúsculas o null si no es válida.</summary>
        string? SafeExtension(string fileName, IEnumerable<string> allowed);
    }
}
