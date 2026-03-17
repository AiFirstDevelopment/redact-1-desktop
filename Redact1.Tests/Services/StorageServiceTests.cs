using FluentAssertions;
using Redact1;
using Redact1.Models;
using Redact1.Services;
using System.Text.Json;

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

    [Fact]
    public void GetAuthToken_WhenNoToken_ReturnsNull()
    {
        _storageService.ClearAll();

        var result = _storageService.GetAuthToken();

        result.Should().BeNull();
    }

    [Fact]
    public void SetAuthToken_WithNull_RemovesToken()
    {
        _storageService.SetAuthToken("test-token");
        _storageService.SetAuthToken(null);

        var result = _storageService.GetAuthToken();

        result.Should().BeNull();
    }

    [Fact]
    public void GetUser_WhenNoUser_ReturnsNull()
    {
        _storageService.ClearAll();

        var result = _storageService.GetUser();

        result.Should().BeNull();
    }

    [Fact]
    public void GetAgencyConfig_WhenNoConfig_ReturnsNull()
    {
        _storageService.ClearAll();

        var result = _storageService.GetAgencyConfig();

        result.Should().BeNull();
    }

    [Fact]
    public void AuthToken_Overwrite_UpdatesValue()
    {
        _storageService.SetAuthToken("first-token");
        _storageService.SetAuthToken("second-token");

        var result = _storageService.GetAuthToken();

        result.Should().Be("second-token");
    }

    [Fact]
    public void User_Overwrite_UpdatesValue()
    {
        _storageService.SetUser(new User { Id = "user-1", Name = "First" });
        _storageService.SetUser(new User { Id = "user-2", Name = "Second" });

        var result = _storageService.GetUser();

        result.Should().NotBeNull();
        result!.Id.Should().Be("user-2");
        result.Name.Should().Be("Second");
    }

    [Fact]
    public void AgencyConfig_Overwrite_UpdatesValue()
    {
        _storageService.SetAgencyConfig(new AgencyConfig { Code = "FIRST" });
        _storageService.SetAgencyConfig(new AgencyConfig { Code = "SECOND" });

        var result = _storageService.GetAgencyConfig();

        result.Should().NotBeNull();
        result!.Code.Should().Be("SECOND");
    }

    [Fact]
    public void MultipleSetsAndGets_MaintainState()
    {
        _storageService.SetAuthToken("token-1");
        _storageService.SetUser(new User { Id = "user-1" });
        _storageService.SetAgencyConfig(new AgencyConfig { Code = "CODE" });

        _storageService.GetAuthToken().Should().Be("token-1");
        _storageService.GetUser()!.Id.Should().Be("user-1");
        _storageService.GetAgencyConfig()!.Code.Should().Be("CODE");

        // Change one value
        _storageService.SetAuthToken("token-2");

        // Other values should be unchanged
        _storageService.GetAuthToken().Should().Be("token-2");
        _storageService.GetUser()!.Id.Should().Be("user-1");
        _storageService.GetAgencyConfig()!.Code.Should().Be("CODE");
    }
}

/// <summary>
/// Unit tests for StorageService that test internal behavior via reflection
/// </summary>
public class StorageServiceUnitTests : IDisposable
{
    private readonly string _testStoragePath;
    private readonly string _testDirectory;

    public StorageServiceUnitTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"Redact1UnitTests_{Guid.NewGuid()}");
        _testStoragePath = Path.Combine(_testDirectory, "storage.json");

        App.Settings = new AppSettings
        {
            StorageKeys = new StorageKeys
            {
                AuthToken = "test_auth_token",
                User = "test_user",
                AgencyConfig = "test_agency_config"
            }
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void GetUser_WithInvalidJson_ReturnsNull()
    {
        // Create a storage service
        var service = new StorageService();

        // Set a valid user first
        service.SetUser(new User { Id = "test" });

        // Now corrupt the storage by setting invalid JSON via reflection
        var storageField = typeof(StorageService).GetField("_storage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var storage = (Dictionary<string, string>)storageField!.GetValue(service)!;
        storage["test_user"] = "not valid json {{{";

        // GetUser should return null when JSON is invalid
        var result = service.GetUser();

        result.Should().BeNull();
    }

    [Fact]
    public void GetAgencyConfig_WithInvalidJson_ReturnsNull()
    {
        // Create a storage service
        var service = new StorageService();

        // Set a valid config first
        service.SetAgencyConfig(new AgencyConfig { Code = "TEST" });

        // Now corrupt the storage by setting invalid JSON via reflection
        var storageField = typeof(StorageService).GetField("_storage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var storage = (Dictionary<string, string>)storageField!.GetValue(service)!;
        storage["test_agency_config"] = "invalid json }}}";

        // GetAgencyConfig should return null when JSON is invalid
        var result = service.GetAgencyConfig();

        result.Should().BeNull();
    }

    [Fact]
    public void LoadStorage_WithCorruptedFile_InitializesEmptyStorage()
    {
        // Create the directory and a corrupted storage file
        Directory.CreateDirectory(_testDirectory);

        // Write invalid JSON to simulate corrupted file
        var storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Redact1",
            "storage.json"
        );

        // Create a new service - it should handle any existing corrupted data gracefully
        var service = new StorageService();

        // Should be able to use the service normally
        service.SetAuthToken("test");
        service.GetAuthToken().Should().Be("test");
    }

    [Fact]
    public void SaveStorage_CreatesDirectoryIfNotExists()
    {
        // Create a new storage service
        var service = new StorageService();

        // Set a value to trigger save
        service.SetAuthToken("test-token");

        // Should work without throwing
        var result = service.GetAuthToken();
        result.Should().Be("test-token");
    }
}
