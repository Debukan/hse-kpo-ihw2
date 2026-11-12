using System.Globalization;
using Pos.Application;
using Pos.Domain;

namespace Pos.Adapters.ConsoleUi;

public sealed class ConsoleApplication(PosFacade pos, TextReader input, TextWriter output)
{
    private readonly ConsoleInput _input = new(input, output);

    public void Run()
    {
        output.WriteLine("ПОС — преподавательская оценка студентов.");

        while (true)
        {
            output.WriteLine("""

                1  Добавить студента
                2  Показать студентов
                3  Добавить критерий
                4  Показать рубрикатор
                5  Выставить или исправить оценку
                6  Показать оценки студента
                7  Отменить оценку
                8  Посчитать итог студента
                9  Посчитать результат по выставленным оценкам
                10 Экспортировать итоги в CSV
                0  Выход
                """);
            output.Write("Выберите действие: ");
            string? command = _input.ReadCommand();
            if (command is null or "0")
            {
                output.WriteLine("Работа завершена.");
                return;
            }

            try
            {
                switch (command)
                {
                    case "1": AddStudent(); break;
                    case "2": ShowStudents(); break;
                    case "3": AddCriterion(); break;
                    case "4": ShowCriteria(); break;
                    case "5": SetEvaluation(); break;
                    case "6": ShowEvaluations(); break;
                    case "7": CancelEvaluation(); break;
                    case "8": ShowResult("Итог", pos.GetFinalResult(ReadStudentId())); break;
                    case "9": ShowResult("Промежуточный результат", pos.GetProgressResult(ReadStudentId())); break;
                    case "10": ExportResults(); break;
                    default: output.WriteLine("Неизвестная команда. Введите число от 0 до 10."); break;
                }
            }
            catch (EndOfStreamException)
            {
                output.WriteLine("\nВвод закончился. Незавершённая команда отменена.");
                return;
            }
            catch (Exception exception) when (exception is DomainException or IOException
                or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                output.WriteLine($"Ошибка: {exception.Message}");
            }
        }
    }

    private void AddStudent()
    {
        int id = ReadStudentId();
        string name = _input.ReadRequired("Имя студента: ");
        pos.AddStudent(id, name);
        output.WriteLine("Студент добавлен.");
    }

    private void ShowStudents()
    {
        var students = pos.GetStudents();
        if (students.Count == 0)
            output.WriteLine("Студентов пока нет.");

        foreach (var student in students)
            output.WriteLine($"№ {student.Id}: {student.Name}");
    }

    private void AddCriterion()
    {
        int id = ReadCriterionId();
        string name = _input.ReadRequired("Название критерия: ");
        decimal maximum = _input.ReadDecimal("Максимальный балл: ");
        decimal weight = _input.ReadDecimal("Вес: ");
        pos.AddCriterion(id, name, maximum, weight);
        output.WriteLine("Критерий добавлен.");
    }

    private void ShowCriteria()
    {
        var criteria = pos.GetCriteria();
        if (criteria.Count == 0)
            output.WriteLine("Рубрикатор пока пуст.");

        foreach (var criterion in criteria)
        {
            output.WriteLine($"№ {criterion.Id}: {criterion.Name}; " +
                $"максимум {Number(criterion.MaximumScore)}, вес {Number(criterion.Weight)}.");
        }
    }

    private void SetEvaluation()
    {
        int studentId = ReadStudentId();
        int criterionId = ReadCriterionId();
        decimal score = _input.ReadDecimal("Балл: ");
        bool replaced = pos.SetEvaluation(studentId, criterionId, score);
        output.WriteLine(replaced ? "Оценка исправлена." : "Оценка выставлена.");
    }

    private void ShowEvaluations()
    {
        var (student, entries) = pos.GetEvaluations(ReadStudentId());
        output.WriteLine($"Оценки: {student.Name} (№ {student.Id}).");
        if (entries.Count == 0)
            output.WriteLine("Оценок пока нет.");

        foreach (var (criterion, evaluation) in entries)
        {
            output.WriteLine($"№ {criterion.Id}: {criterion.Name} — " +
                $"{Number(evaluation.Score)} из {Number(criterion.MaximumScore)}.");
        }
    }

    private void CancelEvaluation()
    {
        bool removed = pos.CancelEvaluation(ReadStudentId(), ReadCriterionId());
        output.WriteLine(removed ? "Оценка отменена." : "По этому критерию оценки нет.");
    }

    private void ShowResult(string heading, StudentResult result)
    {
        var (student, grade) = result;
        string value = grade.Percent is { } percent
            ? percent.ToString("0.##", CultureInfo.InvariantCulture) + "%"
            : grade.CriterionCount == 0 ? "рубрикатор пуст" : "оценок пока нет";
        output.WriteLine($"{heading}: {student.Name} (№ {student.Id}) — {value}. " +
            $"Оценено критериев: {grade.AssessedCount} из {grade.CriterionCount}.");
    }

    private void ExportResults()
    {
        string path = _input.ReadRequired("Путь для нового CSV-файла: ");
        int count = pos.ExportFinalResults(path);
        output.WriteLine($"Экспортировано итогов: {count}. Файл: {path}");
    }

    private int ReadStudentId() => _input.ReadId("Номер студента: ");

    private int ReadCriterionId() => _input.ReadId("Номер критерия: ");

    private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
