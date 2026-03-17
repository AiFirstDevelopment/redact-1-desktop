using Avalonia.Headless.XUnit;
using FluentAssertions;
using Redact1.Tests.Mocks;
using Redact1.Views;

namespace Redact1.Tests.Views;

public class SettingsViewTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public SettingsViewTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true);
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [AvaloniaFact]
    public void Constructor_CreatesView()
    {
        var view = new SettingsView();

        view.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void Initialize_SetsDataContext()
    {
        var view = new SettingsView();

        view.Initialize();

        view.DataContext.Should().NotBeNull();
    }
}
