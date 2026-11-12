namespace Pos.Domain;

public sealed class Student
{
    internal Student(int id, string name)
    {
        if (id <= 0)
            throw new DomainException("Номер студента должен быть положительным.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Имя студента не может быть пустым.");

        Id = id;
        Name = name.Trim();
    }

    public int Id { get; }
    public string Name { get; }
}
