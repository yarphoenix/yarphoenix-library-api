using System.Collections.Concurrent;
using System.Reflection;

namespace YarPhoenix.Shared;

// Обязательные поля тел запросов. System.Text.Json молча оставляет null в ненулевом свойстве,
// если поля в JSON нет, — дальше это NullReferenceException в сервисе и ответ 500. Обязательным
// считается то, что объявлено в контракте без «?»: отдельной разметки не нужно.
public static class RequiredFields
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> RequiredByType = new();

    // Имя первого обязательного свойства, оставшегося без значения; null — всё на месте.
    public static string? FindMissing(object dto)
    {
        foreach (var property in RequiredByType.GetOrAdd(dto.GetType(), FindRequired))
        {
            if (property.GetValue(dto) is null)
            {
                return property.Name;
            }
        }

        return null;
    }

    private static PropertyInfo[] FindRequired(Type type)
    {
        // Контекст не потокобезопасен — свой на каждый разбор типа; результат кэшируется.
        var nullability = new NullabilityInfoContext();

        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead
                && property.GetIndexParameters().Length == 0
                && !property.PropertyType.IsValueType
                && nullability.Create(property).ReadState == NullabilityState.NotNull)
            .ToArray();
    }
}
