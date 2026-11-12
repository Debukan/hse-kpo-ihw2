using Pos.Domain;

namespace Pos.Application;

public sealed record StudentResult(Student Student, GradeSummary Grade);

public interface IFinalResultsExporter
{
    void Export(string destination, IReadOnlyList<StudentResult> results);
}
