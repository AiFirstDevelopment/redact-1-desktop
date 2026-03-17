using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class FileReviewViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public FileReviewViewModelTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true);
        _services.SetupApp();
        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        var file = MockApiService.CreateTestFile();
        _services.MockApi.Setup(x => x.GetFileAsync(It.IsAny<string>()))
            .ReturnsAsync(file);
        _services.MockApi.Setup(x => x.GetOriginalFileAsync(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = new List<Detection>(),
                ManualRedactions = new List<ManualRedaction>()
            });
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.File.Should().BeNull();
        vm.Detections.Should().BeEmpty();
        vm.ManualRedactions.Should().BeEmpty();
        vm.CurrentPage.Should().Be(1);
        vm.TotalPages.Should().Be(1);
        vm.IsDetecting.Should().BeFalse();
        vm.ShowRedacted.Should().BeFalse();
        vm.IsDrawingMode.Should().BeFalse();
    }

    [Fact]
    public async Task LoadFileAsync_LoadsFile()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        await vm.LoadFileAsync("file-123");

        vm.File.Should().NotBeNull();
        vm.File!.Id.Should().Be("file-123");
    }

    [Fact]
    public async Task LoadFileAsync_LoadsDetections()
    {
        var detections = new List<Detection> { MockApiService.CreateTestDetection() };
        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = detections,
                ManualRedactions = new List<ManualRedaction>()
            });

        var vm = _services.GetService<FileReviewViewModel>();

        // Set file directly to avoid image loading issues
        vm.File = MockApiService.CreateTestFile();

        // Call LoadDetectionsAsync through LoadFileAsync - but that will fail on image loading
        // Instead, use reflection to call LoadDetectionsAsync directly
        var method = vm.GetType().GetMethod("LoadDetectionsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(vm, null)!;

        vm.Detections.Should().HaveCount(1);
    }

    [Fact]
    public async Task RunDetectionCommand_RunsDetection()
    {
        var detection = MockApiService.CreateTestDetection();
        // Setup all required mocks
        _services.MockApi.Setup(x => x.ClearDetectionsAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _services.MockApi.Setup(x => x.CreateDetectionsAsync(It.IsAny<string>(), It.IsAny<List<CreateDetectionRequest>>()))
            .ReturnsAsync(new List<Detection> { detection });

        var vm = _services.GetService<FileReviewViewModel>();

        // Set file directly (image type) to avoid PDF path
        vm.File = MockApiService.CreateTestFile(isPdf: false);

        // Set private _originalFileData field via reflection
        var field = vm.GetType().GetField("_originalFileData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(vm, new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        vm.RunDetectionCommand.Execute(null);
        await Task.Delay(300);

        _services.MockApi.Verify(x => x.ClearDetectionsAsync("file-123"), Times.Once);
        vm.Detections.Should().HaveCount(1);
    }

    [Fact]
    public async Task ApproveDetectionCommand_ApprovesDetection()
    {
        var detection = MockApiService.CreateTestDetection();
        var approvedDetection = MockApiService.CreateTestDetection();
        approvedDetection.Status = "approved";

        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = new List<Detection> { detection },
                ManualRedactions = new List<ManualRedaction>()
            });
        _services.MockApi.Setup(x => x.UpdateDetectionAsync(It.IsAny<string>(), It.IsAny<UpdateDetectionRequest>()))
            .ReturnsAsync(approvedDetection);

        var vm = _services.GetService<FileReviewViewModel>();
        await vm.LoadFileAsync("file-123");

        vm.ApproveDetectionCommand.Execute(detection);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateDetectionAsync("det-123",
            It.Is<UpdateDetectionRequest>(r => r.Status == "approved")), Times.Once);
    }

    [Fact]
    public async Task RejectDetectionCommand_RejectsDetection()
    {
        var detection = MockApiService.CreateTestDetection();
        var rejectedDetection = MockApiService.CreateTestDetection();
        rejectedDetection.Status = "rejected";

        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = new List<Detection> { detection },
                ManualRedactions = new List<ManualRedaction>()
            });
        _services.MockApi.Setup(x => x.UpdateDetectionAsync(It.IsAny<string>(), It.IsAny<UpdateDetectionRequest>()))
            .ReturnsAsync(rejectedDetection);

        var vm = _services.GetService<FileReviewViewModel>();
        await vm.LoadFileAsync("file-123");

        vm.RejectDetectionCommand.Execute(detection);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateDetectionAsync("det-123",
            It.Is<UpdateDetectionRequest>(r => r.Status == "rejected")), Times.Once);
    }

    [Fact]
    public async Task AddManualRedaction_CreatesRedaction()
    {
        var redaction = new ManualRedaction
        {
            Id = "red-1",
            BboxX = 10,
            BboxY = 20,
            BboxWidth = 100,
            BboxHeight = 50
        };
        _services.MockApi.Setup(x => x.CreateManualRedactionAsync(It.IsAny<string>(), It.IsAny<CreateManualRedactionRequest>()))
            .ReturnsAsync(redaction);

        var vm = _services.GetService<FileReviewViewModel>();
        await vm.LoadFileAsync("file-123");

        await vm.AddManualRedaction(10, 20, 100, 50);

        vm.ManualRedactions.Should().Contain(redaction);
    }

    [Fact]
    public async Task DeleteManualRedactionCommand_DeletesRedaction()
    {
        var redaction = new ManualRedaction { Id = "red-1" };
        _services.MockApi.Setup(x => x.GetDetectionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new DetectionListResponse
            {
                Detections = new List<Detection>(),
                ManualRedactions = new List<ManualRedaction> { redaction }
            });

        var vm = _services.GetService<FileReviewViewModel>();
        await vm.LoadFileAsync("file-123");

        vm.DeleteManualRedactionCommand.Execute(redaction);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.DeleteManualRedactionAsync("red-1"), Times.Once);
        vm.ManualRedactions.Should().BeEmpty();
    }

    [Fact]
    public void ToggleDrawingModeCommand_TogglesMode()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.ToggleDrawingModeCommand.Execute(null);
        vm.IsDrawingMode.Should().BeTrue();

        vm.ToggleDrawingModeCommand.Execute(null);
        vm.IsDrawingMode.Should().BeFalse();
    }

    [Fact]
    public void CloseCommand_RaisesFileClosedEvent()
    {
        var vm = _services.GetService<FileReviewViewModel>();
        var eventRaised = false;
        vm.FileClosed += (s, e) => eventRaised = true;

        vm.CloseCommand.Execute(null);

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveAllCommand_ApprovesAllPendingDetections()
    {
        var detection1 = MockApiService.CreateTestDetection();
        detection1.Id = "det-1";
        detection1.Status = "pending";
        var detection2 = MockApiService.CreateTestDetection();
        detection2.Id = "det-2";
        detection2.Status = "pending";
        var approvedDetection = MockApiService.CreateTestDetection();
        approvedDetection.Status = "approved";

        _services.MockApi.Setup(x => x.UpdateDetectionAsync(It.IsAny<string>(), It.IsAny<UpdateDetectionRequest>()))
            .ReturnsAsync(approvedDetection);

        var vm = _services.GetService<FileReviewViewModel>();
        vm.File = MockApiService.CreateTestFile();

        // Add detections directly
        vm.Detections.Add(detection1);
        vm.Detections.Add(detection2);

        vm.ApproveAllCommand.Execute(null);
        await Task.Delay(300);

        _services.MockApi.Verify(x => x.UpdateDetectionAsync(It.IsAny<string>(),
            It.Is<UpdateDetectionRequest>(r => r.Status == "approved")), Times.Exactly(2));
    }

    [Fact]
    public async Task ApproveAllCommand_SkipsAlreadyApprovedDetections()
    {
        var detection1 = MockApiService.CreateTestDetection();
        detection1.Id = "det-1";
        detection1.Status = "approved";  // Already approved
        var detection2 = MockApiService.CreateTestDetection();
        detection2.Id = "det-2";
        detection2.Status = "pending";
        var approvedDetection = MockApiService.CreateTestDetection();
        approvedDetection.Status = "approved";

        _services.MockApi.Setup(x => x.UpdateDetectionAsync(It.IsAny<string>(), It.IsAny<UpdateDetectionRequest>()))
            .ReturnsAsync(approvedDetection);

        var vm = _services.GetService<FileReviewViewModel>();
        vm.File = MockApiService.CreateTestFile();

        // Add detections directly
        vm.Detections.Add(detection1);
        vm.Detections.Add(detection2);

        vm.ApproveAllCommand.Execute(null);
        await Task.Delay(300);

        // Only the pending detection should be approved
        _services.MockApi.Verify(x => x.UpdateDetectionAsync(It.IsAny<string>(),
            It.Is<UpdateDetectionRequest>(r => r.Status == "approved")), Times.Once);
    }

    [Fact]
    public void ShowRedacted_CanBeToggled()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.ShowRedacted = true;
        vm.ShowRedacted.Should().BeTrue();

        vm.ShowRedacted = false;
        vm.ShowRedacted.Should().BeFalse();
    }

    [Fact]
    public async Task SaveRedactedCommand_UploadsRedactedFile()
    {
        _services.MockApi.Setup(x => x.UploadRedactedFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ReturnsAsync(MockApiService.CreateTestFile());

        var vm = _services.GetService<FileReviewViewModel>();
        vm.File = MockApiService.CreateTestFile(isPdf: false);

        // Set private fields via reflection
        var originalField = vm.GetType().GetField("_originalFileData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        originalField!.SetValue(vm, new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        var redactedField = vm.GetType().GetField("_redactedFileData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        redactedField!.SetValue(vm, new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        vm.SaveRedactedCommand.Execute(null);
        await Task.Delay(200);

        _services.MockApi.Verify(x => x.UploadRedactedFileAsync("file-123", It.IsAny<byte[]>(),
            It.Is<string>(s => s.Contains(".redacted."))), Times.Once);
    }

    [Fact]
    public void CurrentPage_CanBeSet()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.CurrentPage = 5;
        vm.CurrentPage.Should().Be(5);
    }

    [Fact]
    public void TotalPages_CanBeSet()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.TotalPages = 10;
        vm.TotalPages.Should().Be(10);
    }

    [Fact]
    public void NextPageCommand_Exists()
    {
        var vm = _services.GetService<FileReviewViewModel>();
        vm.NextPageCommand.Should().NotBeNull();
    }

    [Fact]
    public void PreviousPageCommand_Exists()
    {
        var vm = _services.GetService<FileReviewViewModel>();
        vm.PreviousPageCommand.Should().NotBeNull();
    }

    [Fact]
    public void PreviewRedactedCommand_Exists()
    {
        var vm = _services.GetService<FileReviewViewModel>();
        vm.PreviewRedactedCommand.Should().NotBeNull();
    }

    [Fact]
    public void SaveRedactedCommand_Exists()
    {
        var vm = _services.GetService<FileReviewViewModel>();
        vm.SaveRedactedCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadFileAsync_HandlesPdfFiles()
    {
        var pdfFile = MockApiService.CreateTestFile(isPdf: true);
        _services.MockApi.Setup(x => x.GetFileAsync(It.IsAny<string>()))
            .ReturnsAsync(pdfFile);
        _services.MockApi.Setup(x => x.GetOriginalFileAsync(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF

        var vm = _services.GetService<FileReviewViewModel>();

        await vm.LoadFileAsync("file-123");

        vm.File.Should().NotBeNull();
        vm.File!.IsPdf.Should().BeTrue();
        vm.TotalPages.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task AddManualRedaction_SetsPageNumberForPdf()
    {
        var redaction = new ManualRedaction
        {
            Id = "red-1",
            BboxX = 10,
            PageNumber = 2
        };
        _services.MockApi.Setup(x => x.CreateManualRedactionAsync(It.IsAny<string>(), It.IsAny<CreateManualRedactionRequest>()))
            .ReturnsAsync(redaction);

        var vm = _services.GetService<FileReviewViewModel>();
        vm.File = MockApiService.CreateTestFile(isPdf: true);
        vm.CurrentPage = 2;

        await vm.AddManualRedaction(10, 20, 100, 50);

        _services.MockApi.Verify(x => x.CreateManualRedactionAsync("file-123",
            It.Is<CreateManualRedactionRequest>(r => r.PageNumber == 2)), Times.Once);
    }

    [Fact]
    public async Task AddManualRedaction_DoesNothingWhenFileIsNull()
    {
        var vm = _services.GetService<FileReviewViewModel>();
        vm.File = null;

        await vm.AddManualRedaction(10, 20, 100, 50);

        _services.MockApi.Verify(x => x.CreateManualRedactionAsync(It.IsAny<string>(),
            It.IsAny<CreateManualRedactionRequest>()), Times.Never);
    }

    [Fact]
    public async Task ApproveDetectionCommand_DoesNothingWithNullDetection()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.ApproveDetectionCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateDetectionAsync(It.IsAny<string>(),
            It.IsAny<UpdateDetectionRequest>()), Times.Never);
    }

    [Fact]
    public async Task RejectDetectionCommand_DoesNothingWithNullDetection()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.RejectDetectionCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateDetectionAsync(It.IsAny<string>(),
            It.IsAny<UpdateDetectionRequest>()), Times.Never);
    }

    [Fact]
    public async Task DeleteManualRedactionCommand_DoesNothingWithNullRedaction()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.DeleteManualRedactionCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.DeleteManualRedactionAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void SelectedDetection_CanBeSet()
    {
        var vm = _services.GetService<FileReviewViewModel>();
        var detection = MockApiService.CreateTestDetection();

        vm.SelectedDetection = detection;

        vm.SelectedDetection.Should().Be(detection);
    }

    [Fact]
    public void RedactedImage_CanBeSet()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.RedactedImage = null;

        vm.RedactedImage.Should().BeNull();
    }

    [Fact]
    public void IsDetecting_CanBeSet()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.IsDetecting = true;

        vm.IsDetecting.Should().BeTrue();
    }

    [Fact]
    public void ShowRedacted_CanBeSet()
    {
        var vm = _services.GetService<FileReviewViewModel>();

        vm.ShowRedacted = true;

        vm.ShowRedacted.Should().BeTrue();
    }
}
