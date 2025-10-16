using DrugInteractions.Adapters.OpenFda;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DrugInteractions.Tests;

[TestFixture]
public class OpenFdaIntegrationTests : IDisposable
{
    private WireMockServer _mockServer = null!;
    private HttpClient _httpClient = null!;
    private IMemoryCache _cache = null!;
    private OpenFdaSignalAnalyzer _analyzer = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _mockServer = WireMockServer.Start();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_mockServer.Url!)
        };

        var options = Options.Create(new OpenFdaOptions
        {
            BaseUrl = _mockServer.Url!,
            ApiKey = "test-api-key"
        });

        _cache = new MemoryCache(new MemoryCacheOptions());
        _analyzer = new OpenFdaSignalAnalyzer(_httpClient, options, _cache);
    }

    [Test]
    public async Task WhenOpenFdaReturnsResults_ParsesCorrectly()
    {
        // Arrange
        var responseJson = @"{
            ""meta"": {
                ""results"": {
                    ""total"": 157
                }
            },
            ""results"": [
                {
                    ""patient"": {
                        ""drug"": [
                            { ""medicinalproduct"": ""ASPIRIN"" },
                            { ""medicinalproduct"": ""WARFARIN"" }
                        ],
                        ""reaction"": [
                            { ""reactionmeddrapt"": ""BLEEDING"" }
                        ]
                    }
                }
            ]
        }";

        _mockServer
            .Given(
                Request.Create()
                    .WithPath("/drug/event.json")
                    .WithParam("api_key", "test-api-key")
                    .WithParam("search", "patient.drug.medicinalproduct:\"Aspirin\" AND patient.drug.medicinalproduct:\"Warfarin\"")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(responseJson));

        // Act
        var result = await _analyzer.AnalyzeSignalsAsync("Aspirin", "Warfarin", 50);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(157));
            Assert.That(result.TopReactions, Has.Count.EqualTo(1));
            Assert.That(result.TopReactions[0].Reaction, Is.EqualTo("BLEEDING"));
        });
    }

    [OneTimeTearDown]
    public void Dispose()
    {
        _mockServer.Dispose();
        _httpClient.Dispose();
        (_cache as IDisposable)?.Dispose();
    }
}