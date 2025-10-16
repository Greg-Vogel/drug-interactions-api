using System.Web;
using System.Net.Http.Json;
using DrugInteractions.Domain.Models;
using DrugInteractions.Domain.Ports;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DrugInteractions.Adapters.OpenFda;

public class OpenFdaSignalAnalyzer : IDrugSignalAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<OpenFdaOptions> _options;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    public OpenFdaSignalAnalyzer(
        HttpClient httpClient,
        IOptions<OpenFdaOptions> options,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _options = options;
        _cache = cache;
    }

    public async Task<DrugSignalAnalysis> AnalyzeSignalsAsync(
        string drugA,
        string drugB,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Normalize drug order for consistent cache keys
        var (firstDrug, secondDrug) = string.Compare(drugA, drugB, StringComparison.OrdinalIgnoreCase) <= 0 
            ? (drugA, drugB) 
            : (drugB, drugA);

        var cacheKey = $"fda_signal_{firstDrug}_{secondDrug}_{limit}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                entry.SetPriority(CacheItemPriority.Normal);

                var query = HttpUtility.UrlEncode(
                    $"patient.drug.medicinalproduct:\"{firstDrug}\" AND " +
                    $"patient.drug.medicinalproduct:\"{secondDrug}\"");

                var url = $"{_options.Value.BaseUrl}/drug/event.json?" +
                    $"api_key={_options.Value.ApiKey}&search={query}&limit={limit}";

                var response = await _httpClient.GetFromJsonAsync<OpenFdaResponse>(
                    url,
                    cancellationToken);

                if (response == null)
                    throw new Exception("Failed to parse openFDA response");

                var reactionCounts = response.Results
                    .SelectMany(r => r.Patient.Reaction)
                    .GroupBy(r => r.ReactionMeddraPt)
                    .Select(g => new DrugReactionCount(g.Key, g.Count()))
                    .OrderByDescending(r => r.Count)
                    .Take(limit)
                    .ToList();

                return new DrugSignalAnalysis(
                    firstDrug,
                    secondDrug,
                    response.Meta.Results.Total,
                    reactionCounts);
            });
    }
}