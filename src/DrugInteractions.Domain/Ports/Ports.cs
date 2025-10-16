using DrugInteractions.Domain.Models;

namespace DrugInteractions.Domain.Ports;

public interface IDrugInteractionRepository
{
    Task<DrugInteractionNote?> GetInteractionAsync(string drugA, string drugB, CancellationToken cancellationToken = default);
    Task<DrugInteractionNote> UpsertInteractionAsync(string drugA, string drugB, string note, CancellationToken cancellationToken = default);
}

public interface IDrugSignalAnalyzer
{
    Task<DrugSignalAnalysis> AnalyzeSignalsAsync(string drugA, string drugB, int limit = 50, CancellationToken cancellationToken = default);
}