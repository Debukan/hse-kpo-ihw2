using System.Globalization;

namespace Pos.Adapters.ConsoleUi;

internal sealed class ConsoleInput(TextReader reader, TextWriter writer)
{
    public string? ReadCommand() => reader.ReadLine()?.Trim();

    public string ReadRequired(string prompt)
    {
        while (true)
        {
            writer.Write(prompt);
            string value = reader.ReadLine()?.Trim() ?? throw new EndOfStreamException();
            if (value.Length > 0)
                return value;

            writer.WriteLine("Введите непустое значение.");
        }
    }

    public int ReadId(string prompt)
    {
        while (true)
        {
            string value = ReadRequired(prompt);
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int id) && id > 0)
                return id;

            writer.WriteLine("Введите положительное целое число.");
        }
    }

    public decimal ReadDecimal(string prompt)
    {
        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        while (true)
        {
            string value = ReadRequired(prompt).Replace(',', '.');
            if (decimal.TryParse(value, style, CultureInfo.InvariantCulture, out decimal number))
                return number;

            writer.WriteLine("Введите число. Можно использовать точку или запятую.");
        }
    }
}
