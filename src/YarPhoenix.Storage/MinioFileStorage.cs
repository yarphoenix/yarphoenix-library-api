using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace YarPhoenix.Storage;

public sealed class MinioFileStorage(IMinioClient client, IOptions<MinioOptions> options) : IFileStorage
{
    private static readonly Dictionary<string, string> AvatarExtensions = new()
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    private readonly MinioOptions _options = options.Value;

    public async Task<string> UploadBookAsync(
        Stream content,
        long size,
        string contentType,
        CancellationToken cancellationToken)
    {
        await EnsureBucketAsync(_options.BooksBucket, cancellationToken);

        var fileId = $"{Guid.NewGuid():N}.epub";
        await client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_options.BooksBucket)
                .WithObject(fileId)
                .WithStreamData(content)
                .WithObjectSize(size)
                .WithContentType(contentType),
            cancellationToken);

        return fileId;
    }

    public async Task<Stream> OpenBookAsync(string fileId, CancellationToken cancellationToken)
    {
        // GetObjectAsync у MinIO — колбэчный API; буферизуем в MemoryStream.
        // Для EPUB (единицы МБ) этого достаточно; стриминг больших файлов — не наш кейс.
        var buffer = new MemoryStream();
        await client.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_options.BooksBucket)
                .WithObject(fileId)
                .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(buffer, token)),
            cancellationToken);

        buffer.Position = 0;
        return buffer;
    }

    public async Task CopyBookToAsync(string fileId, Stream destination, CancellationToken cancellationToken)
    {
        await client.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_options.BooksBucket)
                .WithObject(fileId)
                .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(destination, token)),
            cancellationToken);
    }

    public async Task DeleteBookAsync(string fileId, CancellationToken cancellationToken)
    {
        await client.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(_options.BooksBucket).WithObject(fileId),
            cancellationToken);
    }

    public async Task<string> UploadAvatarAsync(
        Guid userId,
        Stream content,
        long size,
        string contentType,
        CancellationToken cancellationToken)
    {
        await EnsureAvatarBucketAsync(cancellationToken);

        var extension = AvatarExtensions.GetValueOrDefault(contentType, "jpg");
        var key = $"{userId:N}-{Guid.NewGuid():N}.{extension}";

        await client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_options.AvatarsBucket)
                .WithObject(key)
                .WithStreamData(content)
                .WithObjectSize(size)
                .WithContentType(contentType),
            cancellationToken);

        return key;
    }

    public async Task DeleteAvatarAsync(string key, CancellationToken cancellationToken)
    {
        await client.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(_options.AvatarsBucket).WithObject(key),
            cancellationToken);
    }

    public string BuildAvatarUrl(string key) =>
        $"{_options.PublicBaseUrl.TrimEnd('/')}/{_options.AvatarsBucket}/{key}";

    private async Task EnsureBucketAsync(string bucket, CancellationToken cancellationToken)
    {
        var exists = await client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucket), cancellationToken);
        if (!exists)
        {
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken);
        }
    }

    // Бакет аватаров публичный (ТЗ §5.3): при создании выставляем anonymous-read
    // политику, чтобы <img> грузился без подписи. Политику ставим только на создании.
    private async Task EnsureAvatarBucketAsync(CancellationToken cancellationToken)
    {
        var bucket = _options.AvatarsBucket;
        var exists = await client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucket), cancellationToken);
        if (exists)
        {
            return;
        }

        await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken);

        var policy = $$"""
            {
              "Version": "2012-10-17",
              "Statement": [
                {
                  "Effect": "Allow",
                  "Principal": { "AWS": ["*"] },
                  "Action": ["s3:GetObject"],
                  "Resource": ["arn:aws:s3:::{{bucket}}/*"]
                }
              ]
            }
            """;

        await client.SetPolicyAsync(
            new SetPolicyArgs().WithBucket(bucket).WithPolicy(policy),
            cancellationToken);
    }
}
