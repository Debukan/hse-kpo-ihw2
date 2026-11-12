using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Pos.Adapters.ConsoleUi;
using Pos.Adapters.Export;
using Pos.Adapters.Persistence;
using Pos.Application;
using Pos.Domain;

Console.OutputEncoding = Encoding.UTF8;
string? inputPath = null;
string? dataPath = null;

for (int i = 0; i < args.Length; i += 2)
{
    if (i + 1 == args.Length || string.IsNullOrWhiteSpace(args[i + 1]) ||
        args[i + 1].StartsWith("--", StringComparison.Ordinal))
        return Usage();

    switch (args[i])
    {
        case "--input" when inputPath is null: inputPath = args[i + 1]; break;
        case "--data" when dataPath is null: dataPath = args[i + 1]; break;
        default: return Usage();
    }
}

try
{
    using var inputFile = inputPath is null ? null : File.OpenText(inputPath);
    var services = new ServiceCollection();
    if (dataPath is null)
        services.AddSingleton<IGradebookRepository, InMemoryGradebookRepository>();
    else
        services.AddSingleton<IGradebookRepository>(new JsonGradebookRepository(dataPath));

    services.AddSingleton<IFinalResultsExporter, CsvFinalResultsExporter>();
    services.AddSingleton<GradeCalculator>();
    services.AddSingleton<FinalGradePolicy>();
    services.AddSingleton<ProgressGradePolicy>();
    services.AddSingleton<PosFacade>();
    services.AddSingleton<TextReader>(inputFile is null ? Console.In : inputFile);
    services.AddSingleton<TextWriter>(Console.Out);
    services.AddSingleton<ConsoleApplication>();

    using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    provider.GetRequiredService<ConsoleApplication>().Run();
    return 0;
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
    or ArgumentException or NotSupportedException)
{
    Console.Error.WriteLine($"Ошибка: {exception.Message}");
    return 1;
}

static int Usage()
{
    Console.Error.WriteLine("Запуск: dotnet run --project Pos -- [--input путь] [--data путь]");
    return 1;
}
