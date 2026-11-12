using Pos.Domain;

namespace Pos.Application;

public sealed class PosFacade(
    IGradebookRepository repository,
    IFinalResultsExporter exporter,
    GradeCalculator calculator,
    FinalGradePolicy finalPolicy,
    ProgressGradePolicy progressPolicy)
{
    public IReadOnlyList<Student> GetStudents() => repository.Load().Students;

    public IReadOnlyList<Criterion> GetCriteria() => repository.Load().Criteria;

    public void AddStudent(int id, string name)
    {
        var gradebook = repository.Load();
        gradebook.AddStudent(id, name);
        repository.Save(gradebook);
    }

    public void AddCriterion(int id, string name, decimal maximumScore, decimal weight)
    {
        var gradebook = repository.Load();
        gradebook.AddCriterion(id, name, maximumScore, weight);
        repository.Save(gradebook);
    }

    public bool SetEvaluation(int studentId, int criterionId, decimal score)
    {
        var gradebook = repository.Load();
        bool replaced = gradebook.SetEvaluation(studentId, criterionId, score);
        repository.Save(gradebook);
        return replaced;
    }

    public bool CancelEvaluation(int studentId, int criterionId)
    {
        var gradebook = repository.Load();
        bool removed = gradebook.CancelEvaluation(studentId, criterionId);
        if (removed)
            repository.Save(gradebook);

        return removed;
    }

    public (Student Student, IReadOnlyList<(Criterion Criterion, Evaluation Evaluation)> Entries)
        GetEvaluations(int studentId)
    {
        var gradebook = repository.Load();
        var student = gradebook.GetStudent(studentId);
        var entries = gradebook.GetEvaluations(studentId)
            .Select(evaluation => (gradebook.GetCriterion(evaluation.CriterionId), evaluation))
            .ToArray();
        return (student, entries);
    }

    public StudentResult GetFinalResult(int studentId) => GetResult(studentId, finalPolicy);

    public StudentResult GetProgressResult(int studentId) => GetResult(studentId, progressPolicy);

    public int ExportFinalResults(string destination)
    {
        var gradebook = repository.Load();
        var results = gradebook.Students
            .Select(student => new StudentResult(student, calculator.Calculate(gradebook, student.Id, finalPolicy)))
            .ToArray();

        exporter.Export(destination, results);
        return results.Length;
    }

    private StudentResult GetResult(int studentId, IGradePolicy policy)
    {
        var gradebook = repository.Load();
        var student = gradebook.GetStudent(studentId);
        return new StudentResult(student, calculator.Calculate(gradebook, studentId, policy));
    }
}
