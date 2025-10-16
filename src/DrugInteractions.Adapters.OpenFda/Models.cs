using System.Text.Json.Serialization;

namespace DrugInteractions.Adapters.OpenFda;

public class OpenFdaOptions
{
    public const string ConfigSection = "OpenFda";
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
}

public class OpenFdaResponse
{
    [JsonPropertyName("results")]
    public required List<OpenFdaEvent> Results { get; init; }

    [JsonPropertyName("meta")]
    public required OpenFdaMeta Meta { get; init; }
}

public class OpenFdaEvent
{
    [JsonPropertyName("patient")]
    public required OpenFdaPatient Patient { get; init; }
}

public class OpenFdaPatient
{
    [JsonPropertyName("drug")]
    public required List<OpenFdaDrug> Drug { get; init; }

    [JsonPropertyName("reaction")]
    public required List<OpenFdaReaction> Reaction { get; init; }
}

public class OpenFdaDrug
{
    [JsonPropertyName("medicinalproduct")]
    public required string MedicinalProduct { get; init; }
}

public class OpenFdaReaction
{
    [JsonPropertyName("reactionmeddrapt")]
    public required string ReactionMeddraPt { get; init; }
}

public class OpenFdaMeta
{
    [JsonPropertyName("results")]
    public required OpenFdaResultsMeta Results { get; init; }
}

public class OpenFdaResultsMeta
{
    [JsonPropertyName("total")]
    public required int Total { get; init; }
}