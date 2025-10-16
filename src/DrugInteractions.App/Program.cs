using DrugInteractions.Adapters.Memory;
using DrugInteractions.Adapters.OpenFda;
using DrugInteractions.Adapters.Memory;
using DrugInteractions.Adapters.OpenFda;
using DrugInteractions.Domain.Models;
using DrugInteractions.Domain.Ports;
using DrugInteractions.Domain.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// Configure OpenFDA client
builder.Services.Configure<OpenFdaOptions>(
    builder.Configuration.GetSection(OpenFdaOptions.ConfigSection));
builder.Services.AddHttpClient<IDrugSignalAnalyzer, OpenFdaSignalAnalyzer>();

// Add in-memory repository
builder.Services.AddSingleton<IDrugInteractionRepository, InMemoryDrugInteractionRepository>();

// Add validators
builder.Services.AddScoped<DrugNameValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Configure endpoints
var api = app.MapGroup("/");

api.MapPost("/interactions", async (
    DrugInteractionUpsert request,
    IDrugInteractionRepository repository,
    DrugNameValidator validator,
    CancellationToken cancellationToken) =>
{
    // Run validations in parallel
    var validationTasks = new[]
    {
        validator.ValidateAsync(request.DrugA, cancellationToken),
        validator.ValidateAsync(request.DrugB, cancellationToken)
    };

    var validations = await Task.WhenAll(validationTasks);
    var drugAValidation = validations[0];
    var drugBValidation = validations[1];

    if (!drugAValidation.IsValid || !drugBValidation.IsValid)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["drugA"] = drugAValidation.Errors.Select(e => e.ErrorMessage).ToArray(),
            ["drugB"] = drugBValidation.Errors.Select(e => e.ErrorMessage).ToArray()
        });
    }

    if (string.IsNullOrWhiteSpace(request.Note))
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["note"] = new[] { "Note is required" }
        });

    var result = await repository.UpsertInteractionAsync(
        request.DrugA,
        request.DrugB,
        request.Note,
        cancellationToken);

    return Results.Ok(result);
})
.WithName("UpsertInteraction")
.WithOpenApi();

api.MapGet("/interactions", async (
    string drugA,
    string drugB,
    IDrugInteractionRepository repository,
    DrugNameValidator validator,
    CancellationToken cancellationToken) =>
{
    // Run validations in parallel
    var validationTasks = new[]
    {
        validator.ValidateAsync(drugA, cancellationToken),
        validator.ValidateAsync(drugB, cancellationToken)
    };

    var validations = await Task.WhenAll(validationTasks);
    var drugAValidation = validations[0];
    var drugBValidation = validations[1];

    if (!drugAValidation.IsValid || !drugBValidation.IsValid)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["drugA"] = drugAValidation.Errors.Select(e => e.ErrorMessage).ToArray(),
            ["drugB"] = drugBValidation.Errors.Select(e => e.ErrorMessage).ToArray()
        });
    }

    var result = await repository.GetInteractionAsync(drugA, drugB, cancellationToken);
    return result != null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetInteraction")
.WithOpenApi();

api.MapGet("/signals", async (
    string drugA,
    string drugB,
    int? limit,
    IDrugSignalAnalyzer analyzer,
    DrugNameValidator validator,
    CancellationToken cancellationToken) =>
{
    // Run validations in parallel
    var validationTasks = new[]
    {
        validator.ValidateAsync(drugA, cancellationToken),
        validator.ValidateAsync(drugB, cancellationToken)
    };

    var validations = await Task.WhenAll(validationTasks);
    var drugAValidation = validations[0];
    var drugBValidation = validations[1];

    if (!drugAValidation.IsValid || !drugBValidation.IsValid)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["drugA"] = drugAValidation.Errors.Select(e => e.ErrorMessage).ToArray(),
            ["drugB"] = drugBValidation.Errors.Select(e => e.ErrorMessage).ToArray()
        });
    }

    try
    {
        var result = await analyzer.AnalyzeSignalsAsync(
            drugA,
            drugB,
            limit ?? 50,
            cancellationToken);

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error communicating with openFDA API for drugs {DrugA} and {DrugB}", drugA, drugB);
        return Results.StatusCode(502);
    }
})
.WithName("GetSignals")
.WithOpenApi();

app.Run();

// Request/Response Types
public record DrugInteractionUpsert(string DrugA, string DrugB, string Note);