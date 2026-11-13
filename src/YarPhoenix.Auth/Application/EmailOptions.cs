namespace YarPhoenix.Auth.Application;

// Почтовый провайдер (ТЗ §5.2): пустой ApiKey → dev-режим (ссылки в лог),
// заполненный → отправка через Resend. From без верифицированного домена —
// onboarding@resend.dev (Resend шлёт только на адрес владельца аккаунта).
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string ApiKey { get; set; } = string.Empty;

    public string From { get; set; } = "YarPhoenix Library <onboarding@resend.dev>";
}
