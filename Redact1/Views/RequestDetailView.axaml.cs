using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.ViewModels;

namespace Redact1.Views
{
    public partial class RequestDetailView : UserControl
    {
        private RequestDetailViewModel? _viewModel;

        public event EventHandler<EvidenceFile>? FileSelected;
        public event EventHandler? RequestClosed;

        public RequestDetailView()
        {
            InitializeComponent();

            CloseButton.Click += (s, e) => _viewModel?.CloseCommand.Execute(null);
        }

        public async void LoadRequest(string requestId)
        {
            _viewModel = App.Services.GetRequiredService<RequestDetailViewModel>();
            _viewModel.FileSelected += (s, f) => FileSelected?.Invoke(this, f);
            _viewModel.RequestClosed += (s, e) => RequestClosed?.Invoke(this, e);

            DataContext = _viewModel;

            await _viewModel.LoadRequestAsync(requestId);
        }

        private void FileItem_Click(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is EvidenceFile file)
            {
                _viewModel?.OpenFileCommand.Execute(file);
            }
        }
    }
}
