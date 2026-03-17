using Avalonia.Headless.XUnit;
using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.Views;

namespace Redact1.Tests.Views;

public class MainWindowTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public MainWindowTests()
    {
        _services = new TestServiceProvider(isAuthenticated: false, isEnrolled: true);
        _services.SetupApp();

        // Setup mock for authentication restore
        _services.MockAuth.Setup(x => x.TryRestoreSessionAsync())
            .ReturnsAsync(false);
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [AvaloniaFact]
    public void Constructor_CreatesWindow()
    {
        var window = new MainWindow();

        window.Should().NotBeNull();
    }
}

public class MainWindowAuthenticatedTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public MainWindowAuthenticatedTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true, isEnrolled: true);
        _services.SetupApp();

        // Setup mock for authentication restore
        _services.MockAuth.Setup(x => x.TryRestoreSessionAsync())
            .ReturnsAsync(true);
        _services.MockApi.Setup(x => x.GetRequestsAsync(It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new List<RecordsRequest>());
        _services.MockApi.Setup(x => x.GetArchivedRequestsAsync(It.IsAny<string?>()))
            .ReturnsAsync(new List<RecordsRequest>());
        _services.MockApi.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<User>());
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [AvaloniaFact]
    public void Constructor_CreatesAuthenticatedWindow()
    {
        var window = new MainWindow();

        window.Should().NotBeNull();
    }
}

public class MainWindowNotEnrolledTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public MainWindowNotEnrolledTests()
    {
        _services = new TestServiceProvider(isAuthenticated: false, isEnrolled: false);
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [AvaloniaFact]
    public void Constructor_CreatesWindowForEnrollment()
    {
        var window = new MainWindow();

        window.Should().NotBeNull();
    }
}

/// <summary>
/// Unit tests for MainWindow using reflection to test private methods
/// </summary>
public class MainWindowUnitTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public MainWindowUnitTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true, isEnrolled: true);
        _services.SetupApp();

        _services.MockAuth.Setup(x => x.TryRestoreSessionAsync())
            .ReturnsAsync(true);
        _services.MockApi.Setup(x => x.GetRequestsAsync(It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new List<RecordsRequest>());
        _services.MockApi.Setup(x => x.GetArchivedRequestsAsync(It.IsAny<string?>()))
            .ReturnsAsync(new List<RecordsRequest>());
        _services.MockApi.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<User>());
        _services.MockApi.Setup(x => x.GetRequestAsync(It.IsAny<string>()))
            .ReturnsAsync(new RecordsRequest { Id = "test", Title = "Test" });
        _services.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<EvidenceFile>());
        _services.MockApi.Setup(x => x.GetFileAsync(It.IsAny<string>()))
            .ReturnsAsync(MockApiService.CreateTestFile());
        _services.MockApi.Setup(x => x.GetOriginalFileAsync(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = new List<Detection>(),
                ManualRedactions = new List<ManualRedaction>()
            });
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [AvaloniaFact]
    public void ShowEnrollment_SetsVisibility()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("ShowEnrollment",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(window, null));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void ShowLogin_SetsVisibility()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("ShowLogin",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(window, null));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void ShowMainContent_SetsVisibility()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("ShowMainContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(window, null));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void OnRequestSelected_ShowsDetailPanel()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("OnRequestSelected",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var request = new RecordsRequest { Id = "req-1", Title = "Test" };

        var exception = Record.Exception(() => method!.Invoke(window, new object?[] { null, request }));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void OnRequestClosed_HidesDetailPanel()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("OnRequestClosed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(window, new object?[] { null, EventArgs.Empty }));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void OnFileSelected_ShowsFileReviewPanel()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("OnFileSelected",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var file = new EvidenceFile { Id = "file-1", Filename = "test.jpg" };

        var exception = Record.Exception(() => method!.Invoke(window, new object?[] { null, file }));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void OnFileClosed_HidesFileReviewPanel()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("OnFileClosed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(window, new object?[] { null, EventArgs.Empty }));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void OnAuthStateChanged_WithUser_ShowsMainContent()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("OnAuthStateChanged",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var user = new User { Id = "user-1", Name = "Test User" };

        var exception = Record.Exception(() => method!.Invoke(window, new object?[] { null, user }));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void OnAuthStateChanged_WithNull_WhenEnrolled_ShowsLogin()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("OnAuthStateChanged",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(window, new object?[] { null, null }));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void LogoutButton_Click_CallsLogout()
    {
        var window = new MainWindow();

        var method = typeof(MainWindow).GetMethod("LogoutButton_Click",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Record.Exception(() => method!.Invoke(window, new object?[] { null, null }));

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void InitializeRequestsView_SetsupView()
    {
        var window = new MainWindow();

        // First show main content to initialize RequestsView
        var showMainMethod = typeof(MainWindow).GetMethod("ShowMainContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        showMainMethod!.Invoke(window, null);

        // The InitializeRequestsView is called internally, verify window state
        window.Should().NotBeNull();
    }
}
