using DrugInteractions.Domain.Models;
using DrugInteractions.Domain.Ports;

namespace DrugInteractions.Adapters.Memory;

public class InMemoryDrugInteractionRepository : IDrugInteractionRepository
{
    private readonly Dictionary<(string DrugA, string DrugB), DrugInteractionNote> _interactions = new();

    public Task<DrugInteractionNote?> GetInteractionAsync(string drugA, string drugB, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(drugA, drugB);
        return Task.FromResult(_interactions.GetValueOrDefault(key));
    }

    public Task<DrugInteractionNote> UpsertInteractionAsync(string drugA, string drugB, string note, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(drugA, drugB);
        var now = DateTimeOffset.UtcNow;

        var interaction = _interactions.GetValueOrDefault(key);
        if (interaction == null)
        {
            interaction = new DrugInteractionNote(drugA, drugB, note, now, now);
        }
        else
        {
            interaction = interaction with { Note = note, UpdatedAt = now };
        }

        _interactions[key] = interaction;
        return Task.FromResult(interaction);
    }

    private static (string DrugA, string DrugB) NormalizeKey(string drugA, string drugB)
    {
        // Always store drugs in alphabetical order for consistent key lookup
        return string.Compare(drugA, drugB, StringComparison.OrdinalIgnoreCase) <= 0
            ? (drugA.ToUpperInvariant(), drugB.ToUpperInvariant())
            : (drugB.ToUpperInvariant(), drugA.ToUpperInvariant());
    }
}