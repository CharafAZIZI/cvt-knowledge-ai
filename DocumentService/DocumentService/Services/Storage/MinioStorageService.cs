using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace DocumentService.Services.Storage;

public class MinioOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "cvt-documents";
    public bool UseSSL { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 60;
}

public class MinioStorageService : IFileStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;

    public MinioStorageService(IMinioClient client, IOptions<MinioOptions> options)
    {
        _client = client;
        _bucketName = options.Value.BucketName;
    }

    /// <summary>
    /// Crée le bucket s'il n'existe pas encore. Appelé une fois au démarrage.
    /// </summary>
    public async Task EnsureBucketExistsAsync()
    {
        var exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName));

        if (!exists)
        {
            await _client.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucketName));
        }
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null)
            throw new ArgumentNullException(nameof(fileStream));

        if (!fileStream.CanRead)
            throw new InvalidOperationException("Le flux du fichier n'est pas lisible.");

        var objectSize = fileStream.Length - fileStream.Position;
        if (objectSize < 0)
            throw new InvalidOperationException("Position du flux invalide.");

        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithStreamData(fileStream)
                .WithObjectSize(objectSize)
                .WithContentType(string.IsNullOrWhiteSpace(contentType)
                    ? "application/octet-stream"
                    : contentType),
            cancellationToken);

        return objectKey;
    }

    public async Task<Stream> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        try
        {
            await _client.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectKey)
                    .WithCallbackStream(stream => stream.CopyTo(memoryStream)),
                cancellationToken);

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch
        {
            await memoryStream.DisposeAsync();
            throw;
        }
    }

    public async Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return;

        await _client.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey),
            cancellationToken);
    }
}
