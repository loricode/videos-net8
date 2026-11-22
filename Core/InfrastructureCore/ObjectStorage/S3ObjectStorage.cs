using Amazon.S3;
using Amazon.S3.Model;
using InfrastructureCore.ProcessVideo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfrastructureCore.ObjectStorage
{
    /// <summary>
    /// Proveedor S3 compatible (AWS S3, MinIO, Cloudflare R2, DigitalOcean Spaces…).
    /// FFmpeg trabaja en la carpeta local y el resultado se sube al bucket; las URLs apuntan
    /// a PublicBaseUrl (CDN o endpoint público del bucket).
    /// </summary>
    public sealed class S3ObjectStorage : IObjectStorage, IDisposable
    {
        private readonly IAmazonS3 _s3;
        private readonly ObjectStorageOptions _opt;
        private readonly string _root;
        private readonly ILogger<S3ObjectStorage> _logger;
        private static readonly SemaphoreSlim BucketLock = new(1, 1);
        private static bool _bucketReady;

        public S3ObjectStorage(IOptions<ObjectStorageOptions> options, IOptions<FfmepgOption> ffmpeg, ILogger<S3ObjectStorage> logger)
        {
            _opt = options.Value;
            _root = ffmpeg.Value.PathVideos;
            _logger = logger;

            var config = new AmazonS3Config
            {
                ForcePathStyle = true,
                AuthenticationRegion = _opt.Region,
            };
            if (!string.IsNullOrWhiteSpace(_opt.ServiceUrl)) config.ServiceURL = _opt.ServiceUrl;
            else config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_opt.Region);

            _s3 = new AmazonS3Client(_opt.AccessKey, _opt.SecretKey, config);
        }

        public async Task PublishDirectoryAsync(string localDirectory, string keyPrefix, CancellationToken cancellationToken = default)
        {
            await EnsureBucketAsync(cancellationToken);

            foreach (var file in Directory.EnumerateFiles(localDirectory, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(localDirectory, file).Replace('\\', '/');
                await PutAsync(file, $"{keyPrefix.TrimEnd('/')}/{relative}", cancellationToken);
            }

            if (_opt.DeleteLocalAfterPublish && Directory.Exists(localDirectory))
                Directory.Delete(localDirectory, recursive: true);
        }

        public async Task PublishFileAsync(string localPath, string key, CancellationToken cancellationToken = default)
        {
            await EnsureBucketAsync(cancellationToken);
            await PutAsync(localPath, key, cancellationToken);
            if (_opt.DeleteLocalAfterPublish && File.Exists(localPath)) File.Delete(localPath);
        }

        public async Task DeletePrefixAsync(string keyPrefix, CancellationToken cancellationToken = default)
        {
            var prefix = keyPrefix.TrimEnd('/') + "/";
            string? token = null;
            do
            {
                var list = await _s3.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _opt.Bucket,
                    Prefix = prefix,
                    ContinuationToken = token
                }, cancellationToken);

                var objects = list.S3Objects ?? new List<S3Object>();
                if (objects.Count > 0)
                {
                    await _s3.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = _opt.Bucket,
                        Objects = objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                    }, cancellationToken);
                }
                token = list.IsTruncated == true ? list.NextContinuationToken : null;
            } while (token != null);

            // limpiar copia local de trabajo, si quedó algo
            var local = Path.Combine(_root, keyPrefix);
            if (Directory.Exists(local)) Directory.Delete(local, recursive: true);
        }

        public string GetUrl(string key) => $"{_opt.PublicBaseUrl.TrimEnd('/')}/{key.Replace('\\', '/').TrimStart('/')}";

        private Task PutAsync(string file, string key, CancellationToken ct) =>
            _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _opt.Bucket,
                Key = key,
                FilePath = file,
                ContentType = MediaContentTypes.For(file),
                Headers = { CacheControl = key.EndsWith(".m3u8") ? "public, max-age=60" : "public, max-age=31536000, immutable" }
            }, ct);

        private async Task EnsureBucketAsync(CancellationToken ct)
        {
            if (!_opt.EnsureBucket || _bucketReady) return;
            await BucketLock.WaitAsync(ct);
            try
            {
                if (_bucketReady) return;
                var buckets = await _s3.ListBucketsAsync(ct);
                if (buckets.Buckets?.Any(b => b.BucketName == _opt.Bucket) != true)
                {
                    await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _opt.Bucket }, ct);
                    _logger.LogInformation("Bucket {Bucket} creado.", _opt.Bucket);
                }

                // Lectura pública (los medios se sirven directo desde el bucket/CDN)
                var policy = $$"""
                {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{_opt.Bucket}}/*"]}]}
                """;
                await _s3.PutBucketPolicyAsync(new PutBucketPolicyRequest { BucketName = _opt.Bucket, Policy = policy }, ct);
                _bucketReady = true;
            }
            finally
            {
                BucketLock.Release();
            }
        }

        public void Dispose() => _s3.Dispose();
    }
}
