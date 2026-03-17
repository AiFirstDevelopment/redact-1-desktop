using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using Redact1.ViewModels;

namespace Redact1.Views
{
    public partial class MainWindow : Window
    {
        private readonly IAuthService _authService;
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _authService = App.Services.GetRequiredService<IAuthService>();
            _authService.AuthStateChanged += OnAuthStateChanged;

            Loaded += OnLoaded;
            LogoutButton.Click += LogoutButton_Click;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Check if enrolled first
            if (!_authService.IsEnrolled)
            {
                ShowEnrollment();
                return;
            }

            var restored = await _authService.TryRestoreSessionAsync();

            if (restored)
            {
                ShowMainContent();
            }
            else
            {
                ShowLogin();
            }
        }

        private void OnAuthStateChanged(object? sender, User? user)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (user != null)
                {
                    ShowMainContent();
                }
                else
                {
                    // Check if still enrolled
                    if (_authService.IsEnrolled)
                    {
                        ShowLogin();
                    }
                    else
                    {
                        ShowEnrollment();
                    }
                }
            });
        }

        private void ShowEnrollment()
        {
            EnrollmentView.IsVisible = true;
            LoginView.IsVisible = false;
            MainContent.IsVisible = false;
            DetailPanel.IsVisible = false;
            FileReviewPanel.IsVisible = false;

            var enrollmentViewModel = App.Services.GetRequiredService<EnrollmentViewModel>();
            enrollmentViewModel.EnrollmentComplete += (s, e) => ShowLogin();
            EnrollmentView.DataContext = enrollmentViewModel;
        }

        private void ShowLogin()
        {
            EnrollmentView.IsVisible = false;
            LoginView.IsVisible = true;
            MainContent.IsVisible = false;
            DetailPanel.IsVisible = false;
            FileReviewPanel.IsVisible = false;

            var loginViewModel = App.Services.GetRequiredService<LoginViewModel>();
            loginViewModel.LoginSucceeded += (s, e) => ShowMainContent();
            LoginView.DataContext = loginViewModel;
        }

        private void ShowMainContent()
        {
            EnrollmentView.IsVisible = false;
            LoginView.IsVisible = false;
            MainContent.IsVisible = true;

            _viewModel = App.Services.GetRequiredService<MainViewModel>();
            DataContext = _viewModel;

            UserNameText.Text = _authService.CurrentUser?.Name ?? "User";

            UsersTab.IsVisible = _authService.CurrentUser?.IsSupervisor == true;

            InitializeRequestsView(RequestsView, false);
            InitializeRequestsView(ArchivedView, true);
            UsersView.Initialize();
            SettingsView.Initialize();
        }

        private void InitializeRequestsView(RequestsView view, bool showArchived)
        {
            view.Initialize(showArchived);
            view.RequestSelected += OnRequestSelected;
        }

        private void OnRequestSelected(object? sender, RecordsRequest request)
        {
            DetailPanel.IsVisible = true;
            RequestDetailView.LoadRequest(request.Id);
            RequestDetailView.FileSelected += OnFileSelected;
            RequestDetailView.RequestClosed += OnRequestClosed;
        }

        private void OnRequestClosed(object? sender, EventArgs e)
        {
            DetailPanel.IsVisible = false;
            RequestDetailView.FileSelected -= OnFileSelected;
            RequestDetailView.RequestClosed -= OnRequestClosed;
        }

        private void OnFileSelected(object? sender, EvidenceFile file)
        {
            FileReviewPanel.IsVisible = true;
            FileReviewView.LoadFile(file.Id);
            FileReviewView.FileClosed += OnFileClosed;
        }

        private void OnFileClosed(object? sender, EventArgs e)
        {
            FileReviewPanel.IsVisible = false;
            FileReviewView.FileClosed -= OnFileClosed;
        }

        private async void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            await _authService.LogoutAsync();
        }
    }
}
