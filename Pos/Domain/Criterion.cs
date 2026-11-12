namespace Pos.Domain;

public sealed class Criterion
{
    internal Criterion(int id, string name, decimal maximumScore, decimal weight)
    {
        if (id <= 0)
            throw new DomainException("Номер критерия должен быть положительным.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Название критерия не может быть пустым.");
        if (maximumScore <= 0)
            throw new DomainException("Максимальный балл должен быть положительным.");
        if (weight <= 0)
            throw new DomainException("Вес критерия должен быть положительным.");

        Id = id;
        Name = name.Trim();
        MaximumScore = maximumScore;
        Weight = weight;
    }

    public int Id { get; }
    public string Name { get; }
    public decimal MaximumScore { get; }
    public decimal Weight { get; }
}
