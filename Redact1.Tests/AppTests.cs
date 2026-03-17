using Avalonia.Headless.XUnit;
using FluentAssertions;
using Redact1;
using Redact1.Tests.Mocks;

namespace Redact1.Tests;

public class AppTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public AppTests()
    {
        _services = new TestServiceProvider(isAuthenticated: false);
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Settings_IsNotNull()
    {
        App.Settings.Should().NotBeNull();
    }

    [Fact]
    public void Settings_HasApiSettings()
    {
        App.Settings.ApiSettings.Should().NotBeNull();
    }

    [Fact]
    public void Settings_HasStorageKeys()
    {
        App.Settings.StorageKeys.Should().NotBeNull();
    }

    [Fact]
    public void Services_IsNotNull()
    {
        App.Services.Should().NotBeNull();
    }

    [Fact]
    public void AppSettings_Default_HasCorrectBaseUrl()
    {
        var settings = new AppSettings();

        settings.ApiSettings.BaseUrl.Should().Be("https://redact-1-worker.joelstevick.workers.dev");
    }

    [Fact]
    public void StorageKeys_Default_HasCorrectKeys()
    {
        var keys = new StorageKeys();

        keys.AuthToken.Should().Be("redact1_auth_token");
        keys.User.Should().Be("redact1_user");
        keys.AgencyConfig.Should().Be("redact1_agency_config");
    }

    [Fact]
    public void ApiSettings_Default_HasCorrectBaseUrl()
    {
        var settings = new ApiSettings();

        settings.BaseUrl.Should().Be("https://redact-1-worker.joelstevick.workers.dev");
    }
}
