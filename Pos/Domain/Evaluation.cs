namespace Pos.Domain;

public sealed record Evaluation
{
    private Evaluation(int studentId, int criterionId, decimal score)
    {
        StudentId = studentId;
        CriterionId = criterionId;
        Score = score;
    }

    public int StudentId { get; }
    public int CriterionId { get; }
    public decimal Score { get; }

    internal static Evaluation Create(Student student, Criterion criterion, decimal score)
    {
        if (score < 0 || score > criterion.MaximumScore)
            throw new DomainException($"Балл должен быть в диапазоне от 0 до {criterion.MaximumScore}.");

        return new Evaluation(student.Id, criterion.Id, score);
    }
}
