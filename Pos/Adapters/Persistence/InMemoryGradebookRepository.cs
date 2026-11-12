using Pos.Application;
using Pos.Domain;

namespace Pos.Adapters.Persistence;

public sealed class InMemoryGradebookRepository : IGradebookRepository
{
    private Gradebook _gradebook = new();

    public Gradebook Load() => _gradebook;

    public void Save(Gradebook gradebook) => _gradebook = gradebook;
}
