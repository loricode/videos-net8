namespace InfrastructureCore.ProcessVideo
{
    public class FfmepgOption
    {
        /// <summary>Ruta del ejecutable ffmpeg.</summary>
        public string PathOrigen { get; set; } = string.Empty;
        /// <summary>Ruta de ffprobe (opcional; por defecto junto a ffmpeg).</summary>
        public string PathProbe { get; set; } = string.Empty;
        /// <summary>Carpeta de trabajo: contiene storage/ e images/.</summary>
        public string PathVideos { get; set; } = string.Empty;
    }
}
