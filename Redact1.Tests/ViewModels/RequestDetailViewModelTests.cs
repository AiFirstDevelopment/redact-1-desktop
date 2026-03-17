using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class RequestDetailViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public RequestDetailViewModelTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true);
        _services.SetupApp();
        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        var request = MockApiService.CreateTestRequest();
        _services.MockApi.Setup(x => x.GetRequestAsync(It.IsAny<string>()))
            .ReturnsAsync(request);
        _services.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<EvidenceFile>());
        _services.MockApi.Setup(x => x.GetExportsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Export>());
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var vm = _services.GetService<RequestDetailViewModel>();

        vm.Request.Should().BeNull();
        vm.Files.Should().BeEmpty();
        vm.Exports.Should().BeEmpty();
        vm.Title.Should().BeEmpty();
        vm.Notes.Should().BeEmpty();
        vm.Status.Should().Be("new");
    }

    [Fact]
    public async Task LoadRequestAsync_LoadsRequest()
    {
        var vm = _services.GetService<RequestDetailViewModel>();

        await vm.LoadRequestAsync("req-123");

        vm.Request.Should().NotBeNull();
        vm.Title.Should().Be("Test Request");
        vm.Status.Should().Be("new");
    }

    [Fact]
    public async Task LoadRequestAsync_LoadsFiles()
    {
        var files = new List<EvidenceFile> { MockApiService.CreateTestFile() };
        _services.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(files);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.Files.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadRequestAsync_LoadsExports()
    {
        var exports = new List<Export>
        {
            new Export { Id = "exp-1", Filename = "export.zip" }
        };
        _services.MockApi.Setup(x => x.GetExportsAsync(It.IsAny<string>()))
            .ReturnsAsync(exports);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.Exports.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadRequestAsync_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.GetRequestAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Not found"));

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.ErrorMessage.Should().Contain("Not found");
    }

    [Fact]
    public async Task SaveChangesCommand_UpdatesRequest()
    {
        var updatedRequest = MockApiService.CreateTestRequest();
        updatedRequest.Title = "Updated Title";
        _services.MockApi.Setup(x => x.UpdateRequestAsync(It.IsAny<string>(), It.IsAny<UpdateRequestPayload>()))
            .ReturnsAsync(updatedRequest);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");
        vm.Title = "Updated Title";
        vm.Notes = "Some notes";

        vm.SaveChangesCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateRequestAsync("req-123", It.Is<UpdateRequestPayload>(p =>
            p.Title == "Updated Title" && p.Notes == "Some notes"
        )), Times.Once);
    }

    [Fact]
    public async Task DeleteFileCommand_DeletesFile()
    {
        var file = MockApiService.CreateTestFile();
        _services.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<EvidenceFile> { file });

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.DeleteFileCommand.Execute(file);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.DeleteFileAsync("file-123"), Times.Once);
        vm.Files.Should().BeEmpty();
    }

    [Fact]
    public void OpenFileCommand_RaisesFileSelectedEvent()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var file = MockApiService.CreateTestFile();
        var eventRaised = false;
        EvidenceFile? selectedFile = null;
        vm.FileSelected += (s, f) =>
        {
            eventRaised = true;
            selectedFile = f;
        };

        vm.OpenFileCommand.Execute(file);

        eventRaised.Should().BeTrue();
        selectedFile.Should().Be(file);
        vm.SelectedFile.Should().Be(file);
    }

    [Fact]
    public async Task CreateExportCommand_CreatesExport()
    {
        var export = new Export { Id = "exp-new", Filename = "export.zip" };
        _services.MockApi.Setup(x => x.CreateExportAsync(It.IsAny<string>()))
            .ReturnsAsync(export);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.CreateExportCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.CreateExportAsync("req-123"), Times.Once);
        vm.Exports.Should().Contain(export);
    }

    [Fact]
    public void CloseCommand_RaisesRequestClosedEvent()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var eventRaised = false;
        vm.RequestClosed += (s, e) => eventRaised = true;

        vm.CloseCommand.Execute(null);

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task LoadFilesCommand_WithNullRequest_DoesNothing()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.Request = null;

        vm.LoadFilesCommand.Execute(null);
        await Task.Delay(50);

        _services.MockApi.Verify(x => x.GetFilesAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadExportsCommand_WithNullRequest_DoesNothing()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.Request = null;

        vm.LoadExportsCommand.Execute(null);
        await Task.Delay(50);

        _services.MockApi.Verify(x => x.GetExportsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveChangesCommand_WithNullRequest_DoesNothing()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.Request = null;

        vm.SaveChangesCommand.Execute(null);
        await Task.Delay(50);

        _services.MockApi.Verify(x => x.UpdateRequestAsync(It.IsAny<string>(), It.IsAny<UpdateRequestPayload>()), Times.Never);
    }

    [Fact]
    public async Task CreateExportCommand_WithNullRequest_DoesNothing()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.Request = null;

        vm.CreateExportCommand.Execute(null);
        await Task.Delay(50);

        _services.MockApi.Verify(x => x.CreateExportAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OpenFileCommand_WithNullFile_DoesNothing()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var eventRaised = false;
        vm.FileSelected += (s, f) => eventRaised = true;

        vm.OpenFileCommand.Execute(null);
        await Task.Delay(50);

        eventRaised.Should().BeFalse();
        vm.SelectedFile.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileCommand_WithNullFile_DoesNothing()
    {
        var vm = _services.GetService<RequestDetailViewModel>();

        vm.DeleteFileCommand.Execute(null);
        await Task.Delay(50);

        _services.MockApi.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadFilesAsync_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Failed to load files"));

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.ErrorMessage.Should().Contain("Failed to load files");
    }

    [Fact]
    public async Task LoadExportsAsync_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.GetExportsAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Failed to load exports"));

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.ErrorMessage.Should().Contain("Failed to load exports");
    }

    [Fact]
    public async Task SaveChangesCommand_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.UpdateRequestAsync(It.IsAny<string>(), It.IsAny<UpdateRequestPayload>()))
            .ThrowsAsync(new Exception("Failed to save"));

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.SaveChangesCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Failed to save");
    }

    [Fact]
    public async Task DeleteFileCommand_OnError_SetsErrorMessage()
    {
        var file = MockApiService.CreateTestFile();
        _services.MockApi.Setup(x => x.GetFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<EvidenceFile> { file });
        _services.MockApi.Setup(x => x.DeleteFileAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cannot delete file"));

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.DeleteFileCommand.Execute(file);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Cannot delete file");
    }

    [Fact]
    public async Task CreateExportCommand_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.CreateExportAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Failed to create export"));

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.CreateExportCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Failed to create export");
    }

    [Fact]
    public async Task UploadFileCommand_Completes()
    {
        var vm = _services.GetService<RequestDetailViewModel>();

        vm.UploadFileCommand.Execute(null);
        await Task.Delay(50);

        // Should complete without error (placeholder method)
        vm.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DownloadExportCommand_Completes()
    {
        var export = new Export { Id = "exp-1" };
        var vm = _services.GetService<RequestDetailViewModel>();

        vm.DownloadExportCommand.Execute(export);
        await Task.Delay(50);

        // Should complete without error (placeholder method)
        vm.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public void IsUploading_CanBeSet()
    {
        var vm = _services.GetService<RequestDetailViewModel>();

        vm.IsUploading = true;
        vm.IsUploading.Should().BeTrue();

        vm.IsUploading = false;
        vm.IsUploading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadRequestAsync_SetsNotesFromRequest()
    {
        var request = MockApiService.CreateTestRequest();
        request.Notes = "Test notes";
        _services.MockApi.Setup(x => x.GetRequestAsync(It.IsAny<string>()))
            .ReturnsAsync(request);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.Notes.Should().Be("Test notes");
    }

    [Fact]
    public async Task LoadRequestAsync_HandlesNullNotes()
    {
        var request = MockApiService.CreateTestRequest();
        request.Notes = null;
        _services.MockApi.Setup(x => x.GetRequestAsync(It.IsAny<string>()))
            .ReturnsAsync(request);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.Notes.Should().BeEmpty();
    }

    [Fact]
    public void Files_CanBeSet()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var files = new System.Collections.ObjectModel.ObservableCollection<EvidenceFile>();

        vm.Files = files;

        vm.Files.Should().BeSameAs(files);
    }

    [Fact]
    public void Exports_CanBeSet()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var exports = new System.Collections.ObjectModel.ObservableCollection<Export>();

        vm.Exports = exports;

        vm.Exports.Should().BeSameAs(exports);
    }

    [Fact]
    public void LoadFilesCommand_Exists()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.LoadFilesCommand.Should().NotBeNull();
    }

    [Fact]
    public void LoadExportsCommand_Exists()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.LoadExportsCommand.Should().NotBeNull();
    }

    [Fact]
    public void Title_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.Title))
                propertyChanged = true;
        };

        vm.Title = "New Title";

        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void Notes_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.Notes))
                propertyChanged = true;
        };

        vm.Notes = "New Notes";

        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void Status_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.Status))
                propertyChanged = true;
        };

        vm.Status = "in_progress";

        propertyChanged.Should().BeTrue();
    }

    // Archive and Delete tests

    [Fact]
    public void IsConfirmingDelete_DefaultsToFalse()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.IsConfirmingDelete.Should().BeFalse();
    }

    [Fact]
    public void IsConfirmingDelete_CanBeSet()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.IsConfirmingDelete = true;
        vm.IsConfirmingDelete.Should().BeTrue();
    }

    [Fact]
    public void RequestDeleteCommand_SetsIsConfirmingDeleteToTrue()
    {
        var vm = _services.GetService<RequestDetailViewModel>();

        vm.RequestDeleteCommand.Execute(null);

        vm.IsConfirmingDelete.Should().BeTrue();
    }

    [Fact]
    public void CancelDeleteRequestCommand_SetsIsConfirmingDeleteToFalse()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.IsConfirmingDelete = true;

        vm.CancelDeleteRequestCommand.Execute(null);

        vm.IsConfirmingDelete.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmDeleteRequestCommand_DeletesRequest()
    {
        _services.MockApi.Setup(x => x.DeleteRequestAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");
        vm.IsConfirmingDelete = true;

        vm.ConfirmDeleteRequestCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.DeleteRequestAsync("req-123"), Times.Once);
        vm.IsConfirmingDelete.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmDeleteRequestCommand_RaisesRequestDeletedEvent()
    {
        _services.MockApi.Setup(x => x.DeleteRequestAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");
        var eventRaised = false;
        vm.RequestDeleted += (s, e) => eventRaised = true;

        vm.ConfirmDeleteRequestCommand.Execute(null);
        await Task.Delay(100);

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmDeleteRequestCommand_WithNullRequest_DoesNothing()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.Request = null;

        vm.ConfirmDeleteRequestCommand.Execute(null);
        await Task.Delay(50);

        _services.MockApi.Verify(x => x.DeleteRequestAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmDeleteRequestCommand_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.DeleteRequestAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cannot delete"));

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.ConfirmDeleteRequestCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Cannot delete");
        vm.IsConfirmingDelete.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveRequestCommand_ArchivesRequest()
    {
        var archivedRequest = MockApiService.CreateTestRequest();
        _services.MockApi.Setup(x => x.ArchiveRequestAsync(It.IsAny<string>()))
            .ReturnsAsync(archivedRequest);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.ArchiveRequestCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.ArchiveRequestAsync("req-123"), Times.Once);
    }

    [Fact]
    public async Task ArchiveRequestCommand_RaisesRequestArchivedEvent()
    {
        var archivedRequest = MockApiService.CreateTestRequest();
        _services.MockApi.Setup(x => x.ArchiveRequestAsync(It.IsAny<string>()))
            .ReturnsAsync(archivedRequest);

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");
        var eventRaised = false;
        vm.RequestArchived += (s, e) => eventRaised = true;

        vm.ArchiveRequestCommand.Execute(null);
        await Task.Delay(100);

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task ArchiveRequestCommand_WithNullRequest_DoesNothing()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.Request = null;

        vm.ArchiveRequestCommand.Execute(null);
        await Task.Delay(50);

        _services.MockApi.Verify(x => x.ArchiveRequestAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ArchiveRequestCommand_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.ArchiveRequestAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cannot archive"));

        var vm = _services.GetService<RequestDetailViewModel>();
        await vm.LoadRequestAsync("req-123");

        vm.ArchiveRequestCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Cannot archive");
    }

    [Fact]
    public void ArchiveRequestCommand_Exists()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.ArchiveRequestCommand.Should().NotBeNull();
    }

    [Fact]
    public void RequestDeleteCommand_Exists()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.RequestDeleteCommand.Should().NotBeNull();
    }

    [Fact]
    public void ConfirmDeleteRequestCommand_Exists()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.ConfirmDeleteRequestCommand.Should().NotBeNull();
    }

    [Fact]
    public void CancelDeleteRequestCommand_Exists()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.CancelDeleteRequestCommand.Should().NotBeNull();
    }

    [Fact]
    public void RequestArchived_EventExists()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.RequestArchived += (s, e) => { };
        // Should not throw
    }

    [Fact]
    public void RequestDeleted_EventExists()
    {
        var vm = _services.GetService<RequestDetailViewModel>();
        vm.RequestDeleted += (s, e) => { };
        // Should not throw
    }
}
