namespace Pos.Domain;

public interface IGradePolicy
{
    bool Includes(Evaluation? evaluation);
}

public sealed class FinalGradePolicy : IGradePolicy
{
    public bool Includes(Evaluation? evaluation) => true;
}

public sealed class ProgressGradePolicy : IGradePolicy
{
    public bool Includes(Evaluation? evaluation) => evaluation is not null;
}
