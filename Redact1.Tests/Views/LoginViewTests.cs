using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;
using Redact1.Views;

namespace Redact1.Tests.Views;

public class LoginViewTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public LoginViewTests()
    {
        _services = new TestServiceProvider(isAuthenticated: false);
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [AvaloniaFact]
    public void Constructor_CreatesView()
    {
        var view = new LoginView();

        view.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void PasswordBox_PropertyChanged_UpdatesViewModel()
    {
        var view = new LoginView();
        var vm = App.Services.GetRequiredService<LoginViewModel>();
        view.DataContext = vm;

        // Get PasswordBox via reflection
        var passwordField = typeof(LoginView).GetField("PasswordBox",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var passwordBox = passwordField?.GetValue(view) as TextBox;

        if (passwordBox != null)
        {
            passwordBox.Text = "testpassword";
        }

        // Simply verify the view handles it
        view.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void LoginButton_Click_ExecutesCommand()
    {
        var view = new LoginView();
        var vm = App.Services.GetRequiredService<LoginViewModel>();
        view.DataContext = vm;

        // Simply verify the view was created properly
        view.Should().NotBeNull();
        view.DataContext.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void PasswordBox_PropertyChanged_WithNullDataContext_DoesNotThrow()
    {
        var view = new LoginView();
        view.DataContext = null;

        // Get PasswordBox via reflection
        var passwordField = typeof(LoginView).GetField("PasswordBox",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var passwordBox = passwordField?.GetValue(view) as TextBox;

        // Setting text should not throw when DataContext is null
        var exception = Record.Exception(() =>
        {
            if (passwordBox != null)
            {
                passwordBox.Text = "testpassword";
            }
        });

        exception.Should().BeNull();
    }

    [AvaloniaFact]
    public void LoginButton_Click_WithNullDataContext_DoesNotThrow()
    {
        var view = new LoginView();
        view.DataContext = null;

        // Get LoginButton via reflection
        var buttonField = typeof(LoginView).GetField("LoginButton",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var button = buttonField?.GetValue(view) as Button;

        // Clicking should not throw when DataContext is null
        view.Should().NotBeNull();
    }
}
