# AI Assistant Instructions for drug-interactions-api

This document provides essential context for AI agents working with this codebase.

## Project Overview
- Modern .NET drug interactions API implementing SOLID principles
- Uses minimal APIs for lightweight, efficient endpoint handling
- Integration with Context7 for AI services via MCP (Model Context Protocol)

## Architecture Principles

### API Design
1. RESTful Endpoints
   - Use minimal APIs with typed results for better testability
   - Group related endpoints using `MapGroup()`
   - Include proper HTTP status codes and response types
   - Document accessibility considerations in OpenAPI specs

2. Data Models
   - Use immutable types where possible
   - Implement robust validation using FluentValidation
   - Use UUIDs for entity identification
   - Include audit fields (created/modified timestamps)

3. Service Layer
   - Follow Interface Segregation Principle (ISP)
   - Implement services around business capabilities
   - Use WebClient (HttpClient) over RestTemplate for HTTP calls
   - Handle retries and circuit breaking for external services

### Testing Strategy
1. Unit Testing
   - Use nUnit as test framework
   - Moq for mocking dependencies
   - Follow Arrange-Act-Assert pattern
   - Target 80%+ line/branch coverage for service layer
   ```csharp
   [Test]
   public async Task WhenDrugInteractionFound_ReturnsDetails()
   {
       // Arrange
       var mockRepo = new Mock<IDrugRepository>();
       mockRepo.Setup(r => r.GetInteraction(It.IsAny<string>()))
              .ReturnsAsync(new DrugInteraction { /* ... */ });
       
       var service = new DrugService(mockRepo.Object);

       // Act
       var result = await service.CheckInteraction("drug1", "drug2");

       // Assert
       Assert.That(result, Is.Not.Null);
       mockRepo.Verify(r => r.GetInteraction(It.IsAny<string>()), Times.Once);
   }
   ```

2. Integration Testing
   - Use WireMock.NET for external service mocking
   - Test full request/response cycles
   - Validate OpenAPI spec conformance
   - Include performance benchmarks

### Security
1. Authentication & Authorization
   - JWT-based authentication
   - Role-based access control
   - Input validation and sanitization
   - Rate limiting for API endpoints

2. Data Protection
   - Encrypt sensitive data at rest
   - Use HTTPS only
   - Implement audit logging
   - Follow HIPAA compliance guidelines

## Development Setup

### Required Tools
- .NET SDK 9.0+
- VS Code with C# extension
- Docker for containerization
- Git for version control

### Key Configuration
- `.vscode/mcp.json`: Context7 AI integration settings
- `appsettings.json`: Application configuration
- `.editorconfig`: Code style rules
- `global.json`: SDK version pinning

### Build & Run
```powershell
# Build solution
dotnet build

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Start API locally
dotnet run --project src/DrugInteractions.Api
```

## Best Practices
1. Code Organization
   - Follow Clean Architecture principles
   - Use feature folders for related functionality
   - Keep controllers thin, business logic in services
   - Implement cross-cutting concerns via middleware

2. Error Handling
   - Use custom exception types
   - Include correlation IDs in logs
   - Return problem details for API errors
   - Log exceptions with appropriate context

3. Performance
   - Use async/await consistently
   - Implement caching where appropriate
   - Consider pagination for large datasets
   - Monitor and optimize database queries

## Documentation Requirements
- XML comments on public APIs
- OpenAPI/Swagger documentation with examples
- Include accessibility considerations
- Document rate limits and quotas

Please update these instructions as patterns and conventions evolve during development.