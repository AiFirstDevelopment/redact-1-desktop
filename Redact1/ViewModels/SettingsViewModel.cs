using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

namespace Redact1.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        private User? _currentUser;
        private string _appVersion = string.Empty;

        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        public ICommand LogoutCommand { get; }
        public ICommand OpenSupportCommand { get; }
        public ICommand OpenAboutCommand { get; }

        public event EventHandler? LoggedOut;

        public SettingsViewModel()
        {
            _authService = App.Services.GetRequiredService<IAuthService>();
            CurrentUser = _authService.CurrentUser;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AppVersion = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";

            LogoutCommand = new AsyncRelayCommand(LogoutAsync);
            OpenSupportCommand = new RelayCommand(OpenSupport);
            OpenAboutCommand = new RelayCommand(OpenAbout);
        }

        private async Task LogoutAsync()
        {
            IsLoading = true;

            try
            {
                await _authService.LogoutAsync();
                LoggedOut?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OpenSupport()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "mailto:support@redact1.com",
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore if mailto handler not available
            }
        }

        private void OpenAbout()
        {
            // Placeholder for about dialog
        }
    }
}
