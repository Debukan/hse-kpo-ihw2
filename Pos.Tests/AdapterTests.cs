using Pos.Adapters.ConsoleUi;
using Pos.Adapters.Export;
using Pos.Adapters.Persistence;
using Pos.Application;
using Pos.Domain;

namespace Pos.Tests;

public sealed class AdapterTests : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("pos-tests-");

    [Fact]
    public void JsonRepositoryKeepsTheRubricAndCurrentEvaluationsBetweenInstances()
    {
        string path = TestPath("gradebook.json");
        var repository = new JsonGradebookRepository(path);
        var gradebook = repository.Load();
        gradebook.AddStudent(7, "Анна");
        gradebook.AddCriterion(12, "Проект", 10, 2.5m);
        gradebook.SetEvaluation(7, 12, 4);
        repository.Save(gradebook);

        var reopened = new JsonGradebookRepository(path);
        var restored = reopened.Load();
        Assert.Equal("Анна", restored.GetStudent(7).Name);
        Assert.Equal(2.5m, restored.GetCriterion(12).Weight);
        Assert.Equal(4m, Assert.Single(restored.GetEvaluations(7)).Score);

        restored.CancelEvaluation(7, 12);
        reopened.Save(restored);

        Assert.Empty(repository.Load().GetEvaluations(7));
    }

    [Theory]
    [InlineData("""{"students":[],"criteria":[]}""")]
    [InlineData("""{"students":[null],"criteria":[],"evaluations":[]}""")]
    [InlineData("""{"students":[{"id":1,"name":"Анна"}],"criteria":[],"evaluations":[{"studentId":1,"criterionId":9,"score":1}]}""")]
    public void InvalidJsonCannotLoadAnInvalidGradebook(string json)
    {
        string path = TestPath("bad.json");
        File.WriteAllText(path, json);

        Assert.Throws<InvalidDataException>(() => new JsonGradebookRepository(path).Load());
        Assert.Equal(json, File.ReadAllText(path));
    }

    [Fact]
    public void ConsoleShowsCurrentGradeAndExportsItAfterRejectingAnInvalidEdit()
    {
        string path = TestPath("results.csv");
        using var input = new StringReader($"""
            1
            0
            1
            Анна
            3
            1
            Проект
            10
            2
            5
            1
            1
            7,5
            5
            1
            1
            12
            6
            1
            8
            1
            10
            {path}
            0
            """);
        using var output = new StringWriter();
        var facade = new PosFacade(new InMemoryGradebookRepository(), new CsvFinalResultsExporter(),
            new GradeCalculator(), new FinalGradePolicy(), new ProgressGradePolicy());

        new ConsoleApplication(facade, input, output).Run();

        Assert.Contains("Введите положительное целое число", output.ToString());
        Assert.Contains("Балл должен быть в диапазоне", output.ToString());
        Assert.Contains("Проект — 7.5 из 10", output.ToString());
        Assert.Contains("Итог: Анна (№ 1) — 75%", output.ToString());
        Assert.Contains("Работа завершена", output.ToString());
        Assert.Equal(["student_id;student_name;final_percent;assessed_criteria;total_criteria",
            "1;\"Анна\";75;1;1"], File.ReadAllLines(path));
    }

    [Fact]
    public void CsvEscapesTheNameAndDoesNotOverwriteAnExistingFile()
    {
        string path = TestPath("results.csv");
        var gradebook = new Gradebook();
        gradebook.AddStudent(1, """Анна; "Аня" """);
        gradebook.AddStudent(2, "=1+1");
        var results = gradebook.Students.Select(student => new StudentResult(student, new(null, 0, 0))).ToArray();
        var exporter = new CsvFinalResultsExporter();

        exporter.Export(path, results);

        string[] lines = File.ReadAllLines(path);
        Assert.Equal("1;\"Анна; \"\"Аня\"\"\";;0;0", lines[1]);
        Assert.Equal("2;\"'=1+1\";;0;0", lines[2]);
        Assert.Throws<IOException>(() => exporter.Export(path, []));
        Assert.Equal(lines, File.ReadAllLines(path));
    }

    private string TestPath(string filename) => Path.Combine(_directory.FullName, filename);

    public void Dispose() => _directory.Delete(recursive: true);
}
