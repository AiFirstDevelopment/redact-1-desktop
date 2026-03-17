using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.ViewModels;

namespace Redact1.Views
{
    public partial class RequestsView : UserControl
    {
        private RequestsViewModel? _viewModel;

        public event EventHandler<RecordsRequest>? RequestSelected;

        public RequestsView()
        {
            InitializeComponent();

            CreateButton.Click += (s, e) =>
            {
                if (_viewModel != null)
                {
                    _viewModel.CreateRequestCommand.Execute(null);
                }
            };
        }

        public void Initialize(bool showArchived = false)
        {
            _viewModel = App.Services.GetRequiredService<RequestsViewModel>();
            _viewModel.ShowArchived = showArchived;
            _viewModel.RequestSelected += (s, r) => RequestSelected?.Invoke(this, r);

            DataContext = _viewModel;

            TitleText.Text = showArchived ? "Archived Requests" : "Records Requests";
            CreateButton.IsVisible = !showArchived;
            StatusFilter.IsVisible = !showArchived;

            _ = _viewModel.LoadRequestsAsync();
        }

        private void RequestItem_Click(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is RecordsRequest request)
            {
                _viewModel?.OpenRequestCommand.Execute(request);
            }
        }

        public async Task RefreshAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.LoadRequestsAsync();
            }
        }
    }
}
