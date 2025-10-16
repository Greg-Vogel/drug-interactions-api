namespace DrugInteractions.Domain.Models;

public record DrugInteractionNote(
    string DrugA,
    string DrugB,
    string Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record DrugSignalAnalysis(
    string DrugA,
    string DrugB,
    int Count,
    IReadOnlyList<DrugReactionCount> TopReactions);

public record DrugReactionCount(
    string Reaction,
    int Count);