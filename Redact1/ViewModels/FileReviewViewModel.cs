using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Redact1.ViewModels
{
    public class FileReviewViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly IDetectionService _detectionService;
        private readonly IRedactionService _redactionService;

        private byte[]? _originalFileData;
        private byte[]? _redactedFileData;

        private EvidenceFile? _file;
        private ObservableCollection<Detection> _detections = new();
        private ObservableCollection<ManualRedaction> _manualRedactions = new();
        private Detection? _selectedDetection;
        private Bitmap? _displayImage;
        private Bitmap? _redactedImage;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private bool _isDetecting;
        private bool _showRedacted;
        private bool _isDrawingMode;

        public EvidenceFile? File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }

        public ObservableCollection<Detection> Detections
        {
            get => _detections;
            set => SetProperty(ref _detections, value);
        }

        public ObservableCollection<ManualRedaction> ManualRedactions
        {
            get => _manualRedactions;
            set => SetProperty(ref _manualRedactions, value);
        }

        public Detection? SelectedDetection
        {
            get => _selectedDetection;
            set => SetProperty(ref _selectedDetection, value);
        }

        public Bitmap? DisplayImage
        {
            get => _displayImage;
            set => SetProperty(ref _displayImage, value);
        }

        public Bitmap? RedactedImage
        {
            get => _redactedImage;
            set => SetProperty(ref _redactedImage, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public bool IsDetecting
        {
            get => _isDetecting;
            set => SetProperty(ref _isDetecting, value);
        }

        public bool ShowRedacted
        {
            get => _showRedacted;
            set => SetProperty(ref _showRedacted, value);
        }

        public bool IsDrawingMode
        {
            get => _isDrawingMode;
            set => SetProperty(ref _isDrawingMode, value);
        }

        public ICommand LoadDetectionsCommand { get; }
        public ICommand RunDetectionCommand { get; }
        public ICommand ApproveDetectionCommand { get; }
        public ICommand RejectDetectionCommand { get; }
        public ICommand ApproveAllCommand { get; }
        public ICommand DeleteManualRedactionCommand { get; }
        public ICommand PreviewRedactedCommand { get; }
        public ICommand SaveRedactedCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ToggleDrawingModeCommand { get; }
        public ICommand CloseCommand { get; }

        public event EventHandler? FileClosed;

        public FileReviewViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();
            _detectionService = App.Services.GetRequiredService<IDetectionService>();
            _redactionService = App.Services.GetRequiredService<IRedactionService>();

            LoadDetectionsCommand = new AsyncRelayCommand(LoadDetectionsAsync);
            RunDetectionCommand = new AsyncRelayCommand(RunDetectionAsync);
            ApproveDetectionCommand = new AsyncRelayCommand<Detection>(ApproveDetectionAsync);
            RejectDetectionCommand = new AsyncRelayCommand<Detection>(RejectDetectionAsync);
            ApproveAllCommand = new AsyncRelayCommand(ApproveAllAsync);
            DeleteManualRedactionCommand = new AsyncRelayCommand<ManualRedaction>(DeleteManualRedactionAsync);
            PreviewRedactedCommand = new AsyncRelayCommand(PreviewRedactedAsync);
            SaveRedactedCommand = new AsyncRelayCommand(SaveRedactedAsync);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
            ToggleDrawingModeCommand = new RelayCommand(ToggleDrawingMode);
            CloseCommand = new RelayCommand(Close);
        }

        public async Task LoadFileAsync(string fileId)
        {
            IsLoading = true;
            ClearError();

            try
            {
                File = await _apiService.GetFileAsync(fileId);
                _originalFileData = await _apiService.GetOriginalFileAsync(fileId);

                if (File.IsPdf)
                {
                    TotalPages = _redactionService.GetPdfPageCount(_originalFileData);
                    await LoadPdfPage(1);
                }
                else
                {
                    await LoadImage();
                }

                await LoadDetectionsAsync();
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadImage()
        {
            if (_originalFileData == null) return;

            await Task.Run(() =>
            {
                using var ms = new MemoryStream(_originalFileData);
                var bitmap = new Bitmap(ms);

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    DisplayImage = bitmap;
                });
            });
        }

        private async Task LoadPdfPage(int pageNumber)
        {
            if (_originalFileData == null) return;

            var pageImage = await _redactionService.RenderPdfPageToImageAsync(_originalFileData, pageNumber);

            using var ms = new MemoryStream(pageImage);
            DisplayImage = new Bitmap(ms);
            CurrentPage = pageNumber;
        }

        private async Task LoadDetectionsAsync()
        {
            if (File == null) return;

            try
            {
                var result = await _apiService.GetDetectionsAsync(File.Id);

                Detections.Clear();
                foreach (var detection in result.Detections)
                {
                    Detections.Add(detection);
                }

                ManualRedactions.Clear();
                foreach (var redaction in result.ManualRedactions)
                {
                    ManualRedactions.Add(redaction);
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private async Task RunDetectionAsync()
        {
            if (File == null || _originalFileData == null) return;

            IsDetecting = true;
            ClearError();

            try
            {
                await _apiService.ClearDetectionsAsync(File.Id);
                Detections.Clear();

                List<CreateDetectionRequest> allDetections = new();

                if (File.IsImage)
                {
                    var detected = await _detectionService.DetectInImageAsync(_originalFileData);
                    allDetections.AddRange(detected);
                }
                else if (File.IsPdf)
                {
                    for (int page = 1; page <= TotalPages; page++)
                    {
                        var pageImage = await _redactionService.RenderPdfPageToImageAsync(_originalFileData, page);
                        var detected = await _detectionService.DetectInPdfPageAsync(pageImage, page);
                        allDetections.AddRange(detected);
                    }
                }

                if (allDetections.Count > 0)
                {
                    var created = await _apiService.CreateDetectionsAsync(File.Id, allDetections);
                    foreach (var detection in created)
                    {
                        Detections.Add(detection);
                    }
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsDetecting = false;
            }
        }

        private async Task ApproveDetectionAsync(Detection? detection)
        {
            if (detection == null) return;

            try
            {
                var updated = await _apiService.UpdateDetectionAsync(
                    detection.Id,
                    new UpdateDetectionRequest { Status = "approved" }
                );

                var index = Detections.IndexOf(detection);
                if (index >= 0)
                {
                    Detections[index] = updated;
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private async Task RejectDetectionAsync(Detection? detection)
        {
            if (detection == null) return;

            try
            {
                var updated = await _apiService.UpdateDetectionAsync(
                    detection.Id,
                    new UpdateDetectionRequest { Status = "rejected" }
                );

                var index = Detections.IndexOf(detection);
                if (index >= 0)
                {
                    Detections[index] = updated;
                }
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private async Task ApproveAllAsync()
        {
            foreach (var detection in Detections.Where(d => d.Status == "pending").ToList())
            {
                await ApproveDetectionAsync(detection);
            }
        }

        public async Task AddManualRedaction(double x, double y, double width, double height)
        {
            if (File == null) return;

            try
            {
                var request = new CreateManualRedactionRequest
                {
                    BboxX = x,
                    BboxY = y,
                    BboxWidth = width,
                    BboxHeight = height,
                    PageNumber = File.IsPdf ? CurrentPage : null
                };

                var redaction = await _apiService.CreateManualRedactionAsync(File.Id, request);
                ManualRedactions.Add(redaction);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private async Task DeleteManualRedactionAsync(ManualRedaction? redaction)
        {
            if (redaction == null) return;

            try
            {
                await _apiService.DeleteManualRedactionAsync(redaction.Id);
                ManualRedactions.Remove(redaction);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private async Task PreviewRedactedAsync()
        {
            if (File == null || _originalFileData == null) return;

            IsLoading = true;

            try
            {
                byte[] redactedData;

                if (File.IsImage)
                {
                    redactedData = await _redactionService.RedactImageAsync(
                        _originalFileData,
                        Detections.ToList(),
                        ManualRedactions.ToList()
                    );
                }
                else
                {
                    redactedData = await _redactionService.RedactPdfAsync(
                        _originalFileData,
                        Detections.ToList(),
                        ManualRedactions.ToList()
                    );
                }

                _redactedFileData = redactedData;

                if (File.IsImage)
                {
                    using var ms = new MemoryStream(redactedData);
                    RedactedImage = new Bitmap(ms);
                }

                ShowRedacted = true;
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveRedactedAsync()
        {
            if (File == null || _redactedFileData == null) return;

            IsLoading = true;

            try
            {
                var filename = File.IsPdf
                    ? Path.ChangeExtension(File.Filename, ".redacted.pdf")
                    : Path.ChangeExtension(File.Filename, ".redacted.jpg");

                await _apiService.UploadRedactedFileAsync(File.Id, _redactedFileData, filename);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                await LoadPdfPage(CurrentPage + 1);
            }
        }

        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                await LoadPdfPage(CurrentPage - 1);
            }
        }

        private void ToggleDrawingMode()
        {
            IsDrawingMode = !IsDrawingMode;
        }

        private void Close()
        {
            FileClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
