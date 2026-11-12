using Pos.Domain;

namespace Pos.Tests;

public sealed class GradeCalculatorTests
{
    private readonly GradeCalculator _calculator = new();
    private readonly FinalGradePolicy _final = new();
    private readonly ProgressGradePolicy _progress = new();

    [Fact]
    public void FinalNormalizesDifferentMaximumsAndUsesRelativeWeights()
    {
        var gradebook = CreateGradebook();
        gradebook.SetEvaluation(1, 1, 8);
        gradebook.SetEvaluation(1, 2, 15);
        gradebook.SetEvaluation(2, 1, 10);

        Assert.Equal(new GradeSummary(77m, 2, 2), _calculator.Calculate(gradebook, 1, _final));
        Assert.Equal(new GradeSummary(40m, 1, 2), _calculator.Calculate(gradebook, 2, _final));
    }

    [Fact]
    public void MissingEvaluationCountsAsZeroOnlyInTheFinal()
    {
        var gradebook = CreateGradebook();
        gradebook.SetEvaluation(1, 1, 8);

        Assert.Equal(new GradeSummary(32m, 1, 2), _calculator.Calculate(gradebook, 1, _final));
        Assert.Equal(new GradeSummary(80m, 1, 2), _calculator.Calculate(gradebook, 1, _progress));

        gradebook.SetEvaluation(1, 2, 0);
        Assert.Equal(new GradeSummary(32m, 2, 2), _calculator.Calculate(gradebook, 1, _progress));
    }

    [Fact]
    public void RecalculationUsesTheCurrentRubricAndCurrentEvaluations()
    {
        var gradebook = CreateGradebook();
        gradebook.SetEvaluation(1, 1, 8);
        gradebook.SetEvaluation(1, 1, 5);
        gradebook.SetEvaluation(1, 2, 20);
        gradebook.CancelEvaluation(1, 2);
        gradebook.AddCriterion(3, "Устный ответ", 5, 5);

        Assert.Equal(new GradeSummary(10m, 1, 3), _calculator.Calculate(gradebook, 1, _final));
    }

    [Fact]
    public void EmptyRubricAndNoProgressHaveNoPercentage()
    {
        var empty = new Gradebook();
        empty.AddStudent(1, "Анна");
        var gradebook = CreateGradebook();

        Assert.Equal(new GradeSummary(null, 0, 0), _calculator.Calculate(empty, 1, _final));
        Assert.Equal(new GradeSummary(null, 0, 2), _calculator.Calculate(gradebook, 1, _progress));
        Assert.Equal(new GradeSummary(0m, 0, 2), _calculator.Calculate(gradebook, 1, _final));
    }

    [Fact]
    public void ExtremeWeightsDoNotOverflowOrTurnAValidResultIntoZero()
    {
        var gradebook = new Gradebook();
        gradebook.AddStudent(1, "Анна");
        gradebook.AddCriterion(1, "Задания", 10, decimal.MaxValue);
        gradebook.AddCriterion(2, "Проект", 20, decimal.MaxValue);
        gradebook.AddCriterion(3, "Ответ", 2, 0.0000000000000000000000000001m);
        gradebook.SetEvaluation(1, 1, 5);
        gradebook.SetEvaluation(1, 2, 20);

        Assert.Equal(75m, _calculator.Calculate(gradebook, 1, _final).Percent);

        gradebook.CancelEvaluation(1, 1);
        gradebook.CancelEvaluation(1, 2);
        gradebook.SetEvaluation(1, 3, 1);

        Assert.Equal(50m, _calculator.Calculate(gradebook, 1, _progress).Percent);
    }

    [Fact]
    public void PercentIsRoundedToTwoDecimalPlaces()
    {
        var gradebook = new Gradebook();
        gradebook.AddStudent(1, "Анна");
        gradebook.AddCriterion(1, "Задания", 6, 1);
        gradebook.SetEvaluation(1, 1, 1);

        Assert.Equal(16.67m, _calculator.Calculate(gradebook, 1, _final).Percent);
    }

    [Fact]
    public void MidpointPercentRoundsAwayFromZero()
    {
        var gradebook = new Gradebook();
        gradebook.AddStudent(1, "Анна");
        gradebook.AddCriterion(1, "Задания", 8, 1);
        gradebook.AddCriterion(2, "Проект", 3, 3);
        gradebook.SetEvaluation(1, 1, 7);
        gradebook.SetEvaluation(1, 2, 1);

        Assert.Equal(46.88m, _calculator.Calculate(gradebook, 1, _final).Percent);
    }

    private static Gradebook CreateGradebook()
    {
        var gradebook = new Gradebook();
        gradebook.AddStudent(1, "Анна");
        gradebook.AddStudent(2, "Илья");
        gradebook.AddCriterion(1, "Задания", 10, 2);
        gradebook.AddCriterion(2, "Проект", 20, 3);
        return gradebook;
    }
}
