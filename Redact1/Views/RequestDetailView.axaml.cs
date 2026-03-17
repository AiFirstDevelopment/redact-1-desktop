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
            Console.WriteLine("[Upload] Button clicked");

            if (_viewModel?.Request == null)
            {
                Console.WriteLine("[Upload] No request loaded");
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                Console.WriteLine("[Upload] No top level window");
                return;
            }

            Console.WriteLine("[Upload] Opening file picker...");
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select files to upload",
                AllowMultiple = true
            });

            Console.WriteLine($"[Upload] Selected {files.Count} files");
            if (files.Count == 0) return;

            _viewModel.IsUploading = true;

            try
            {
                var apiService = App.Services.GetRequiredService<IApiService>();

                foreach (var file in files)
                {
                    var localPath = file.TryGetLocalPath();
                    Console.WriteLine($"[Upload] File: {file.Name}, LocalPath: {localPath ?? "NULL"}");

                    if (localPath != null)
                    {
                        var uploaded = await apiService.UploadFileAsync(_viewModel.Request.Id, localPath);
                        Console.WriteLine($"[Upload] Success: {uploaded.Filename}");
                        _viewModel.Files.Add(uploaded);
                    }
                    else
                    {
                        Console.WriteLine($"[Upload] No local path - reading stream...");
                        // Read file bytes directly for sandboxed environments
                        await using var stream = await file.OpenReadAsync();
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);
                        var bytes = ms.ToArray();
                        Console.WriteLine($"[Upload] Read {bytes.Length} bytes from {file.Name}");

                        // Save to temp file and upload
                        var tempPath = Path.Combine(Path.GetTempPath(), file.Name);
                        await File.WriteAllBytesAsync(tempPath, bytes);
                        Console.WriteLine($"[Upload] Saved to temp: {tempPath}");

                        var uploaded = await apiService.UploadFileAsync(_viewModel.Request.Id, tempPath);
                        Console.WriteLine($"[Upload] Success: {uploaded.Filename}");
                        _viewModel.Files.Add(uploaded);

                        // Clean up temp file
                        File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Upload] Error: {ex.Message}");
                Console.WriteLine($"[Upload] Stack: {ex.StackTrace}");
            }
            finally
            {
                _viewModel.IsUploading = false;
            }
        }

        public async void LoadRequest(string requestId)
        {
            Console.WriteLine($"[Detail] LoadRequest called: {requestId}");
            _viewModel = App.Services.GetRequiredService<RequestDetailViewModel>();
            _viewModel.FileSelected += (s, f) => FileSelected?.Invoke(this, f);
            _viewModel.RequestClosed += (s, e) => RequestClosed?.Invoke(this, e);
            _viewModel.RequestArchived += (s, e) => RequestArchived?.Invoke(this, e);
            _viewModel.RequestDeleted += (s, e) => RequestDeleted?.Invoke(this, e);

            DataContext = _viewModel;

            await _viewModel.LoadRequestAsync(requestId);
            Console.WriteLine($"[Detail] Request loaded: {_viewModel.Request?.Id ?? "NULL"}");
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
