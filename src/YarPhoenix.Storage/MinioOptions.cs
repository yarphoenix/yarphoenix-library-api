namespace YarPhoenix.Storage;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = "127.0.0.1:9000";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool UseSsl { get; set; }

    // Два бакета с разными политиками доступа (ТЗ §5.3): books — приватный,
    // avatars — публичный (подключается с аватарами).
    public string BooksBucket { get; set; } = "books";

    public string AvatarsBucket { get; set; } = "avatars";

    // Публичный адрес MinIO для ссылок на аватары. В dev — прямой :9000,
    // в проде — домен за Caddy (см. деплой). Отделён от Endpoint (внутренний адрес
    // для SDK), который недоступен снаружи.
    public string PublicBaseUrl { get; set; } = "http://127.0.0.1:9000";
}
