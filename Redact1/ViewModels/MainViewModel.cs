using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Windows.Input;

namespace Redact1.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private User? _currentUser;
        private int _selectedTabIndex;

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                SetProperty(ref _currentUser, value);
                OnPropertyChanged(nameof(IsSupervisor));
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public bool IsSupervisor => CurrentUser?.IsSupervisor ?? false;

        public ICommand LogoutCommand { get; }

        public event EventHandler? LoggedOut;

        public MainViewModel()
        {
            _authService = App.Services.GetRequiredService<IAuthService>();
            CurrentUser = _authService.CurrentUser;
            LogoutCommand = new AsyncRelayCommand(LogoutAsync);

            _authService.AuthStateChanged += (s, user) =>
            {
                CurrentUser = user;
                if (user == null)
                {
                    LoggedOut?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        private async Task LogoutAsync()
        {
            IsLoading = true;
            try
            {
                await _authService.LogoutAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
