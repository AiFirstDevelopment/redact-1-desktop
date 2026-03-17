using FluentAssertions;
using Redact1;
using Redact1.Models;
using Redact1.Services;

namespace Redact1.Tests.Services;

public class StorageServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly StorageService _storageService;

    public StorageServiceTests()
    {
        // Setup App.Settings for the tests
        App.Settings = new AppSettings
        {
            StorageKeys = new StorageKeys
            {
                AuthToken = "test_auth_token",
                User = "test_user",
                AgencyConfig = "test_agency_config"
            }
        };

        // Use a temp directory for storage tests
        _tempPath = Path.Combine(Path.GetTempPath(), $"Redact1Tests_{Guid.NewGuid()}");

        // StorageService will use the actual path, but we test the interface behavior
        _storageService = new StorageService();
    }

    public void Dispose()
    {
        // Cleanup
        _storageService.ClearAll();
    }

    [Fact]
    public void AuthToken_SetAndGet_Works()
    {
        _storageService.SetAuthToken("test-token-123");

        var result = _storageService.GetAuthToken();

        result.Should().Be("test-token-123");
    }

    [Fact]
    public void AuthToken_Clear_RemovesToken()
    {
        _storageService.SetAuthToken("test-token");
        _storageService.ClearAuthToken();

        var result = _storageService.GetAuthToken();

        result.Should().BeNull();
    }

    [Fact]
    public void User_SetAndGet_Works()
    {
        var user = new User { Id = "user-1", Name = "Test User", Email = "test@test.com" };

        _storageService.SetUser(user);
        var result = _storageService.GetUser();

        result.Should().NotBeNull();
        result!.Id.Should().Be("user-1");
        result.Name.Should().Be("Test User");
    }

    [Fact]
    public void User_SetNull_ClearsUser()
    {
        var user = new User { Id = "user-1" };
        _storageService.SetUser(user);
        _storageService.SetUser(null);

        var result = _storageService.GetUser();

        result.Should().BeNull();
    }

    [Fact]
    public void User_Clear_RemovesUser()
    {
        _storageService.SetUser(new User { Id = "user-1" });
        _storageService.ClearUser();

        var result = _storageService.GetUser();

        result.Should().BeNull();
    }

    [Fact]
    public void AgencyConfig_SetAndGet_Works()
    {
        var config = new AgencyConfig { Code = "DEMO", Name = "Demo PD" };

        _storageService.SetAgencyConfig(config);
        var result = _storageService.GetAgencyConfig();

        result.Should().NotBeNull();
        result!.Code.Should().Be("DEMO");
    }

    [Fact]
    public void AgencyConfig_SetNull_ClearsConfig()
    {
        _storageService.SetAgencyConfig(new AgencyConfig { Code = "TEST" });
        _storageService.SetAgencyConfig(null);

        var result = _storageService.GetAgencyConfig();

        result.Should().BeNull();
    }

    [Fact]
    public void AgencyConfig_Clear_RemovesConfig()
    {
        _storageService.SetAgencyConfig(new AgencyConfig { Code = "TEST" });
        _storageService.ClearAgencyConfig();

        var result = _storageService.GetAgencyConfig();

        result.Should().BeNull();
    }

    [Fact]
    public void ClearAll_RemovesEverything()
    {
        _storageService.SetAuthToken("token");
        _storageService.SetUser(new User { Id = "user-1" });
        _storageService.SetAgencyConfig(new AgencyConfig { Code = "TEST" });

        _storageService.ClearAll();

        _storageService.GetAuthToken().Should().BeNull();
        _storageService.GetUser().Should().BeNull();
        _storageService.GetAgencyConfig().Should().BeNull();
    }
}
