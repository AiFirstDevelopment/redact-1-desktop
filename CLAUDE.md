# Redact-1 Windows/Mac Desktop App

Cross-platform Avalonia UI application for managing FOIA/records requests with PII redaction.

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run --project Redact1/Redact1.csproj

# Test
dotnet test Redact1.Tests/Redact1.Tests.csproj
```

## Testing Guidelines

**IMPORTANT: All tests must use the integration test framework with Avalonia.Headless - no unit tests.**

### Test Architecture
- Use `TestServiceProvider` to create a fully wired DI container with mock services
- Tests run against the actual ViewModels with mocked backend services (IApiService, IAuthService, etc.)
- Use `Avalonia.Headless.XUnit` for any UI component testing
- Mock services are in `Redact1.Tests/Mocks/`

### Test Patterns
```csharp
public class MyViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public MyViewModelTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true);
        _services.SetupApp();  // Wires up App.Services for integration
    }

    public void Dispose() => _services.Dispose();

    [Fact]
    public async Task SomeTest()
    {
        var vm = _services.GetService<MyViewModel>();
        // Test against fully integrated ViewModel
    }
}
```

### Pre-commit Hook
A pre-commit hook runs all tests before each commit. Tests must pass to commit.

## Architecture

- **Redact1/** - Main Avalonia app
  - `Models/` - Data models with JSON serialization
  - `Services/` - Business logic and API clients
  - `ViewModels/` - MVVM ViewModels with INotifyPropertyChanged
  - `Views/` - Avalonia XAML views
- **Redact1.Tests/** - Integration tests

## API

Backend: `https://redact-1-worker.joelstevick.workers.dev`

API responses are wrapped:
- Lists: `{ "requests": [...] }`, `{ "files": [...] }`
- Single items: `{ "request": {...} }`
