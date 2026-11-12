using System.Text.Json;
using Pos.Application;
using Pos.Domain;

namespace Pos.Adapters.Persistence;

public sealed class JsonGradebookRepository(string path) : IGradebookRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true
    };

    private readonly string _path = Path.GetFullPath(path);

    public Gradebook Load()
    {
        var gradebook = new Gradebook();

        try
        {
            using var input = File.OpenRead(_path);
            var snapshot = JsonSerializer.Deserialize<Snapshot>(input, Options)
                ?? throw new JsonException("Ожидался объект ведомости.");

            foreach (var student in snapshot.Students)
            {
                var row = Required(student);
                gradebook.AddStudent(row.Id, row.Name);
            }

            foreach (var criterion in snapshot.Criteria)
            {
                var row = Required(criterion);
                gradebook.AddCriterion(row.Id, row.Name, row.MaximumScore, row.Weight);
            }

            foreach (var evaluation in snapshot.Evaluations)
            {
                var row = Required(evaluation);
                if (gradebook.SetEvaluation(row.StudentId, row.CriterionId, row.Score))
                    throw new JsonException("В ведомости есть повторяющаяся оценка.");
            }

            return gradebook;
        }
        catch (FileNotFoundException)
        {
            return gradebook;
        }
        catch (Exception exception) when (exception is JsonException or DomainException)
        {
            throw new InvalidDataException($"Некорректный файл ведомости: {exception.Message}", exception);
        }
    }

    public void Save(Gradebook gradebook)
    {
        var snapshot = new Snapshot(
            gradebook.Students.Select(student => new StudentRow(student.Id, student.Name)).ToArray(),
            gradebook.Criteria.Select(criterion =>
                new CriterionRow(criterion.Id, criterion.Name, criterion.MaximumScore, criterion.Weight)).ToArray(),
            gradebook.Evaluations.Select(evaluation =>
                new EvaluationRow(evaluation.StudentId, evaluation.CriterionId, evaluation.Score)).ToArray());

        string temporaryPath = Path.Combine(Path.GetDirectoryName(_path)!, Path.GetRandomFileName());
        var output = new FileStream(temporaryPath, FileMode.CreateNew);
        try
        {
            using (output)
                JsonSerializer.Serialize(output, snapshot, Options);

            File.Move(temporaryPath, _path, overwrite: true);
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }

    private static T Required<T>(T? row) where T : class =>
        row ?? throw new JsonException("В списках ведомости не может быть пустых записей.");

    private sealed record Snapshot(StudentRow?[] Students, CriterionRow?[] Criteria, EvaluationRow?[] Evaluations);
    private sealed record StudentRow(int Id, string Name);
    private sealed record CriterionRow(int Id, string Name, decimal MaximumScore, decimal Weight);
    private sealed record EvaluationRow(int StudentId, int CriterionId, decimal Score);
}
