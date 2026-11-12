namespace Pos.Domain;

public sealed class Gradebook
{
    private readonly Dictionary<int, Student> _students = new();
    private readonly Dictionary<int, Criterion> _criteria = new();
    private readonly Dictionary<(int StudentId, int CriterionId), Evaluation> _evaluations = new();

    public IReadOnlyList<Student> Students => _students.Values.OrderBy(student => student.Id).ToArray();
    public IReadOnlyList<Criterion> Criteria => _criteria.Values.OrderBy(criterion => criterion.Id).ToArray();

    public IReadOnlyList<Evaluation> Evaluations => _evaluations.Values
        .OrderBy(evaluation => evaluation.StudentId)
        .ThenBy(evaluation => evaluation.CriterionId)
        .ToArray();

    public void AddStudent(int id, string name)
    {
        var student = new Student(id, name);
        if (!_students.TryAdd(id, student))
            throw new DomainException($"Студент с номером {id} уже есть в ведомости.");
    }

    public void AddCriterion(int id, string name, decimal maximumScore, decimal weight)
    {
        var criterion = new Criterion(id, name, maximumScore, weight);
        if (!_criteria.TryAdd(id, criterion))
            throw new DomainException($"Критерий с номером {id} уже есть в рубрикаторе.");
    }

    public bool SetEvaluation(int studentId, int criterionId, decimal score)
    {
        var student = GetStudent(studentId);
        var criterion = GetCriterion(criterionId);
        var evaluation = Evaluation.Create(student, criterion, score);

        var key = (studentId, criterionId);
        bool replaced = _evaluations.ContainsKey(key);
        _evaluations[key] = evaluation;
        return replaced;
    }

    public bool CancelEvaluation(int studentId, int criterionId)
    {
        GetStudent(studentId);
        GetCriterion(criterionId);
        return _evaluations.Remove((studentId, criterionId));
    }

    public Student GetStudent(int id) => _students.TryGetValue(id, out var student)
        ? student
        : throw new DomainException($"Студент с номером {id} не найден.");

    public Criterion GetCriterion(int id) => _criteria.TryGetValue(id, out var criterion)
        ? criterion
        : throw new DomainException($"Критерий с номером {id} не найден в рубрикаторе.");

    public IReadOnlyList<Evaluation> GetEvaluations(int studentId)
    {
        GetStudent(studentId);
        return _evaluations.Values
            .Where(evaluation => evaluation.StudentId == studentId)
            .OrderBy(evaluation => evaluation.CriterionId)
            .ToArray();
    }
}
