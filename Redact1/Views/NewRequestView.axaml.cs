using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.ViewModels;

namespace Redact1.Views
{
    public partial class NewRequestView : UserControl
    {
        private NewRequestViewModel? _viewModel;

        public event EventHandler? Cancelled;
        public event EventHandler<RecordsRequest>? RequestCreated;

        public NewRequestView()
        {
            InitializeComponent();

            CancelButton.Click += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);
            CreateButton.Click += CreateButton_Click;
            GenerateButton.Click += (s, e) => _viewModel?.GenerateRequestNumber();
        }

        public void Initialize()
        {
            _viewModel = App.Services.GetRequiredService<NewRequestViewModel>();
            DataContext = _viewModel;
        }

        private async void CreateButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var request = await _viewModel.CreateRequestAsync();
            if (request != null)
            {
                RequestCreated?.Invoke(this, request);
            }
        }
    }
}
