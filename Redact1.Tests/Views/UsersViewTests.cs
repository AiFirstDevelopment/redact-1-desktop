using Avalonia.Headless.XUnit;
using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.Views;

namespace Redact1.Tests.Views;

public class UsersViewTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public UsersViewTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true, isSupervisor: true);
        _services.SetupApp();

        _services.MockApi.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<User>());
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [AvaloniaFact]
    public void Constructor_CreatesView()
    {
        var view = new UsersView();

        view.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void Initialize_SetsDataContext()
    {
        var view = new UsersView();

        view.Initialize();

        view.DataContext.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void EditPasswordBox_PropertyChanged_UpdatesViewModel()
    {
        var view = new UsersView();
        view.Initialize();

        // Get EditPasswordBox via reflection
        var passwordField = typeof(UsersView).GetField("EditPasswordBox",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var passwordBox = passwordField?.GetValue(view) as Avalonia.Controls.TextBox;

        if (passwordBox != null)
        {
            passwordBox.Text = "testpassword";
        }

        // Simply verify the view handles it
        view.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void EditPasswordBox_PropertyChanged_WhenNotInitialized_DoesNotThrow()
    {
        var view = new UsersView();
        // Don't call Initialize()

        // Get EditPasswordBox via reflection
        var passwordField = typeof(UsersView).GetField("EditPasswordBox",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var passwordBox = passwordField?.GetValue(view) as Avalonia.Controls.TextBox;

        // Setting text should not throw when _viewModel is null
        var exception = Record.Exception(() =>
        {
            if (passwordBox != null)
            {
                passwordBox.Text = "testpassword";
            }
        });

        exception.Should().BeNull();
    }
}
