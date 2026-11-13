using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace YarPhoenix.Storage;

public static class StorageModule
{
    public static IServiceCollection AddStorageModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));

        services.AddSingleton<IMinioClient>(_ =>
        {
            var options = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>()
                ?? new MinioOptions();
            return new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(options.UseSsl)
                .Build();
        });

        services.AddSingleton<IFileStorage, MinioFileStorage>();

        return services;
    }
}
