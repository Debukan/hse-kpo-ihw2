namespace Pos.Domain;

public sealed record GradeSummary(decimal? Percent, int AssessedCount, int CriterionCount);

public sealed class GradeCalculator
{
    public GradeSummary Calculate(Gradebook gradebook, int studentId, IGradePolicy policy)
    {
        var evaluations = gradebook.GetEvaluations(studentId)
            .ToDictionary(evaluation => evaluation.CriterionId);
        var criteria = gradebook.Criteria;
        var included = criteria
            .Select(criterion => (Criterion: criterion, Evaluation: evaluations.GetValueOrDefault(criterion.Id)))
            .Where(entry => policy.Includes(entry.Evaluation))
            .ToArray();

        if (included.Length == 0)
            return new GradeSummary(null, evaluations.Count, criteria.Count);

        decimal percent = Round(Percent(included));
        return new GradeSummary(percent, evaluations.Count, criteria.Count);
    }

    private static decimal Percent((Criterion Criterion, Evaluation? Evaluation)[] included)
    {
        try
        {
            decimal direct = DirectPercent(included);
            if (direct == 0m && included.Any(entry => (entry.Evaluation?.Score ?? 0m) > 0m))
                return ScaledPercent(included);

            return direct;
        }
        catch (OverflowException)
        {
            return ScaledPercent(included);
        }
    }

    private static decimal DirectPercent((Criterion Criterion, Evaluation? Evaluation)[] included)
    {
        decimal weightSum = 0m;
        decimal weighted = 0m;
        foreach (var (criterion, evaluation) in included)
        {
            decimal score = evaluation?.Score ?? 0m;
            weightSum += criterion.Weight;
            weighted += criterion.Weight * score / criterion.MaximumScore;
        }

        return weighted / weightSum * 100m;
    }

    private static decimal ScaledPercent((Criterion Criterion, Evaluation? Evaluation)[] included)
    {
        decimal scale = included.Max(entry => entry.Criterion.Weight);
        decimal weightSum = 0m;
        decimal weighted = 0m;
        foreach (var (criterion, evaluation) in included)
        {
            decimal weight = criterion.Weight / scale;
            decimal score = evaluation?.Score ?? 0m;
            weightSum += weight;
            weighted += weight * score / criterion.MaximumScore;
        }

        return weighted / weightSum * 100m;
    }

    private static decimal Round(decimal percent) =>
        Math.Round(percent, 2, MidpointRounding.AwayFromZero);
}
