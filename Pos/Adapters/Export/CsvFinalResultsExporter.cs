using System.Globalization;
using System.Text;
using Pos.Application;

namespace Pos.Adapters.Export;

public sealed class CsvFinalResultsExporter : IFinalResultsExporter
{
    public void Export(string destination, IReadOnlyList<StudentResult> results)
    {
        using var file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write);
        using var output = new StreamWriter(file, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        output.WriteLine("student_id;student_name;final_percent;assessed_criteria;total_criteria");

        foreach (var (student, grade) in results)
        {
            string percent = grade.Percent?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
            output.WriteLine(FormattableString.Invariant(
                $"{student.Id};{QuoteName(student.Name)};{percent};{grade.AssessedCount};{grade.CriterionCount}"));
        }
    }

    private static string QuoteName(string name)
    {
        if (name[0] is '=' or '+' or '-' or '@')
            name = "'" + name;

        return "\"" + name.Replace("\"", "\"\"") + "\"";
    }
}
