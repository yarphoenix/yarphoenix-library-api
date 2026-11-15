using System.Text;

namespace YarPhoenix.Catalog.Infrastructure.Collection;

// Чтение CSV по RFC 4180: поле в кавычках может содержать запятые, переводы строк и удвоенные
// кавычки. В каталоге Project Gutenberg заголовки книг многострочные — построчное чтение их рвёт.
internal static class CsvReader
{
    public static IEnumerable<List<string>> Read(TextReader reader)
    {
        var record = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var quoteSeen = false;
        var recordStarted = false;

        int next;
        while ((next = reader.Read()) >= 0)
        {
            var character = (char)next;

            if (inQuotes)
            {
                if (!quoteSeen)
                {
                    if (character == '"')
                    {
                        quoteSeen = true;
                    }
                    else
                    {
                        field.Append(character);
                    }

                    continue;
                }

                quoteSeen = false;
                if (character == '"')
                {
                    // Удвоенная кавычка внутри поля — это один символ кавычки.
                    field.Append('"');
                    continue;
                }

                // Кавычка была закрывающей; текущий символ разбирается как обычный.
                inQuotes = false;
            }

            switch (character)
            {
                case '"':
                    inQuotes = true;
                    recordStarted = true;
                    break;
                case ',':
                    record.Add(field.ToString());
                    field.Clear();
                    recordStarted = true;
                    break;
                case '\r':
                    break;
                case '\n':
                    if (recordStarted)
                    {
                        record.Add(field.ToString());
                        field.Clear();
                        yield return record;
                        record = [];
                        recordStarted = false;
                    }

                    break;
                default:
                    field.Append(character);
                    recordStarted = true;
                    break;
            }
        }

        if (recordStarted)
        {
            record.Add(field.ToString());
            yield return record;
        }
    }
}
