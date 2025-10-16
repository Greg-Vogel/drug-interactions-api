using DrugInteractions.Domain.Models;
using DrugInteractions.Domain.Ports;
using DrugInteractions.Domain.Validation;
using DrugInteractions.Adapters.OpenFda;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Net.Http.Json;

namespace DrugInteractions.Tests;

public class ValidationTests
{
    private DrugNameValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new DrugNameValidator();
    }

    [Test]
    [TestCase("Aspirin", true)]
    [TestCase("Co-Codamol", true)]
    [TestCase("Vitamin B-12", true)]
    [TestCase("a", false)] // Too short
    [TestCase("ThisDrugNameIsMuchTooLongAndShouldFailValidationBecauseItExceedsTheMaximumLength", false)]
    [TestCase("Invalid#Name", false)]
    [TestCase("123", false)]
    public async Task ValidateDrugName(string input, bool shouldBeValid)
    {
        var result = await _validator.ValidateAsync(input);
        Assert.That(result.IsValid, Is.EqualTo(shouldBeValid));
    }
}

[TestFixture]
public class DrugInteractionTests
{
    private Mock<IDrugInteractionRepository> _mockRepository = null!;
    private DrugNameValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IDrugInteractionRepository>();
        _validator = new DrugNameValidator();
    }

    [Test]
    public async Task WhenInteractionExists_ReturnsNote()
    {
        // Arrange
        var expected = new DrugInteractionNote(
            "Aspirin",
            "Warfarin",
            "Test note",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        _mockRepository
            .Setup(r => r.GetInteractionAsync("Aspirin", "Warfarin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _mockRepository.Object.GetInteractionAsync("Aspirin", "Warfarin");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DrugA, Is.EqualTo("Aspirin"));
        Assert.That(result.DrugB, Is.EqualTo("Warfarin"));
    }

    [Test]
    public async Task WhenInteractionDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetInteractionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DrugInteractionNote?)null);

        // Act
        var result = await _mockRepository.Object.GetInteractionAsync("Aspirin", "Warfarin");

        // Assert
        Assert.That(result, Is.Null);
    }
}

[TestFixture]
public class OpenFdaSignalAnalyzerTests
{
    private Mock<HttpClient> _mockHttpClient = null!;
    private Mock<IOptions<OpenFdaOptions>> _mockOptions = null!;
    private IMemoryCache _cache = null!;
    private OpenFdaSignalAnalyzer _analyzer = null!;

    [SetUp]
    public void Setup()
    {
        _mockHttpClient = new Mock<HttpClient>();
        _mockOptions = new Mock<IOptions<OpenFdaOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(new OpenFdaOptions
        {
            BaseUrl = "https://api.fda.gov",
            ApiKey = "test-key"
        });
        
        _cache = new MemoryCache(new MemoryCacheOptions());
        _analyzer = new OpenFdaSignalAnalyzer(_mockHttpClient.Object, _mockOptions.Object, _cache);
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
    }

    [Test]
    public async Task WhenCalledMultipleTimes_UsesCachedResults()
    {
        // Arrange
        var drugA = "Aspirin";
        var drugB = "Warfarin";
        var limit = 50;

        var apiCallCount = 0;
        _mockHttpClient
            .Setup(c => c.GetFromJsonAsync<OpenFdaResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string url, CancellationToken ct) =>
            {
                apiCallCount++;
                return new OpenFdaResponse
                {
                    Meta = new OpenFdaMeta { Results = new OpenFdaResultsMeta { Total = 1 } },
                    Results = new List<OpenFdaEvent>()
                };
            });

        // Act
        var result1 = await _analyzer.AnalyzeSignalsAsync(drugA, drugB, limit);
        var result2 = await _analyzer.AnalyzeSignalsAsync(drugA, drugB, limit);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(apiCallCount, Is.EqualTo(1), "API should only be called once due to caching");
            Assert.That(result2, Is.EqualTo(result1), "Second call should return same cached result");
        });
    }
}