namespace YarPhoenix.Auth.Application;

// База для ссылок в письмах (подтверждение почты, сброс пароля) — фронтенд
// живёт на другом origin (ТЗ §11.1), поэтому адрес конфигурируемый.
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; set; } = "http://localhost:3000";
}
