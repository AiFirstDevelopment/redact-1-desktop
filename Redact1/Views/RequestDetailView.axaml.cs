using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using Redact1.ViewModels;

namespace Redact1.Views
{
    public partial class RequestDetailView : UserControl
    {
        private RequestDetailViewModel? _viewModel;

        public event EventHandler<EvidenceFile>? FileSelected;
        public event EventHandler? RequestClosed;
        public event EventHandler? RequestArchived;
        public event EventHandler? RequestDeleted;

        public RequestDetailView()
        {
            InitializeComponent();

            CloseButton.Click += (s, e) => _viewModel?.CloseCommand.Execute(null);
            UploadButton.Click += UploadButton_Click;
        }

        private async void UploadButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.Request == null) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select files to upload",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Documents") { Patterns = new[] { "*.pdf", "*.png", "*.jpg", "*.jpeg" } },
                    FilePickerFileTypes.All
                }
            });

            if (files.Count == 0) return;

            _viewModel.IsUploading = true;

            try
            {
                var apiService = App.Services.GetRequiredService<IApiService>();

                foreach (var file in files)
                {
                    // Get the local file path
                    var localPath = file.TryGetLocalPath();
                    if (localPath != null)
                    {
                        var uploaded = await apiService.UploadFileAsync(_viewModel.Request.Id, localPath);
                        _viewModel.Files.Add(uploaded);
                    }
                }
            }
            catch (Exception)
            {
                // Error handling - could show a dialog
            }
            finally
            {
                _viewModel.IsUploading = false;
            }
        }

        public async void LoadRequest(string requestId)
        {
            _viewModel = App.Services.GetRequiredService<RequestDetailViewModel>();
            _viewModel.FileSelected += (s, f) => FileSelected?.Invoke(this, f);
            _viewModel.RequestClosed += (s, e) => RequestClosed?.Invoke(this, e);
            _viewModel.RequestArchived += (s, e) => RequestArchived?.Invoke(this, e);
            _viewModel.RequestDeleted += (s, e) => RequestDeleted?.Invoke(this, e);

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
