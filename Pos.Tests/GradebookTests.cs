using Pos.Domain;

namespace Pos.Tests;

public sealed class GradebookTests
{
    private readonly Gradebook _gradebook = CreateGradebook();

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void InvalidScoreDoesNotReplaceAnExistingEvaluation(int score)
    {
        _gradebook.SetEvaluation(1, 7, 8);

        Assert.Throws<DomainException>(() => _gradebook.SetEvaluation(1, 7, score));

        Assert.Equal(8m, Assert.Single(_gradebook.GetEvaluations(1)).Score);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void BothEndsOfTheScoreRangeAreAccepted(int score)
    {
        Assert.False(_gradebook.SetEvaluation(1, 7, score));

        Assert.Equal(score, Assert.Single(_gradebook.GetEvaluations(1)).Score);
    }

    [Theory]
    [InlineData(999, 7)]
    [InlineData(1, 999)]
    public void AnEvaluationRequiresAKnownStudentAndCriterion(int studentId, int criterionId)
    {
        Assert.Throws<DomainException>(() => _gradebook.SetEvaluation(studentId, criterionId, 5));

        Assert.Empty(_gradebook.Evaluations);
    }

    [Fact]
    public void CorrectingAnEvaluationDoesNotCreateASecondOneAndItCanBeCancelled()
    {
        Assert.False(_gradebook.SetEvaluation(1, 7, 4m));
        Assert.True(_gradebook.SetEvaluation(1, 7, 6.5m));
        Assert.Equal(6.5m, Assert.Single(_gradebook.GetEvaluations(1)).Score);

        Assert.True(_gradebook.CancelEvaluation(1, 7));
        Assert.False(_gradebook.CancelEvaluation(1, 7));
        Assert.Empty(_gradebook.GetEvaluations(1));
    }

    [Fact]
    public void DuplicateIdsDoNotReplaceTheStudentOrCriterion()
    {
        Assert.Throws<DomainException>(() => _gradebook.AddStudent(1, "Другой студент"));
        Assert.Throws<DomainException>(() => _gradebook.AddCriterion(7, "Другой критерий", 20, 5));

        Assert.Equal("Анна", Assert.Single(_gradebook.Students).Name);
        Assert.Equal(10m, Assert.Single(_gradebook.Criteria).MaximumScore);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(-1, 2)]
    [InlineData(10, 0)]
    [InlineData(10, -1)]
    public void MaximumAndWeightMustBePositive(int maximum, int weight)
    {
        Assert.Throws<DomainException>(() => _gradebook.AddCriterion(8, "Проект", maximum, weight));

        Assert.Single(_gradebook.Criteria);
    }

    [Fact]
    public void StudentsHavePositiveIdsAndNonBlankNames()
    {
        Assert.Throws<DomainException>(() => _gradebook.AddStudent(0, "Илья"));
        Assert.Throws<DomainException>(() => _gradebook.AddStudent(2, "  "));

        Assert.Single(_gradebook.Students);
    }

    private static Gradebook CreateGradebook()
    {
        var gradebook = new Gradebook();
        gradebook.AddStudent(1, "Анна");
        gradebook.AddCriterion(7, "Задания", 10, 2);
        return gradebook;
    }
}
