using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class RequestsViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public RequestsViewModelTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true);
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var vm = _services.GetService<RequestsViewModel>();

        vm.Requests.Should().BeEmpty();
        vm.SearchText.Should().BeEmpty();
        vm.StatusFilter.Should().Be("all");
        vm.ShowArchived.Should().BeFalse();
        vm.CreateRequestCommand.Should().NotBeNull();
        vm.OpenRequestCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadRequestsAsync_LoadsRequests()
    {
        var requests = new List<RecordsRequest>
        {
            MockApiService.CreateTestRequest(),
            MockApiService.CreateTestRequest()
        };
        _services.MockApi.Setup(x => x.GetRequestsAsync(It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(requests);

        var vm = _services.GetService<RequestsViewModel>();
        await vm.LoadRequestsAsync();

        vm.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadRequestsAsync_ShowArchived_LoadsArchivedRequests()
    {
        var requests = new List<RecordsRequest> { MockApiService.CreateTestRequest() };
        _services.MockApi.Setup(x => x.GetArchivedRequestsAsync(It.IsAny<string?>()))
            .ReturnsAsync(requests);

        var vm = _services.GetService<RequestsViewModel>();
        vm.ShowArchived = true;
        await vm.LoadRequestsAsync();

        _services.MockApi.Verify(x => x.GetArchivedRequestsAsync(null), Times.Once);
        vm.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadRequestsAsync_WithStatusFilter_FiltersRequests()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.StatusFilter = "in_progress";

        // Wait for the debounced load
        await Task.Delay(200);

        _services.MockApi.Verify(x => x.GetRequestsAsync("in_progress", null), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoadRequestsAsync_WithSearchText_SearchesRequests()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.SearchText = "FOIA-2024";

        await Task.Delay(200);

        _services.MockApi.Verify(x => x.GetRequestsAsync(null, "FOIA-2024"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoadRequestsAsync_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.GetRequestsAsync(It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Network error"));

        var vm = _services.GetService<RequestsViewModel>();
        await vm.LoadRequestsAsync();

        vm.ErrorMessage.Should().Contain("Network error");
    }

    [Fact]
    public void CreateRequestCommand_RaisesNewRequestRequestedEvent_Legacy()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var eventRaised = false;
        vm.NewRequestRequested += (s, e) => eventRaised = true;

        vm.CreateRequestCommand.Execute(null);

        eventRaised.Should().BeTrue();
        // No longer creates request directly - now shows form first
        _services.MockApi.Verify(x => x.CreateRequestAsync(It.IsAny<CreateRequestPayload>()), Times.Never);
    }

    [Fact]
    public void OpenRequestCommand_RaisesRequestSelectedEvent()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var request = MockApiService.CreateTestRequest();
        var eventRaised = false;
        RecordsRequest? selectedRequest = null;
        vm.RequestSelected += (s, r) =>
        {
            eventRaised = true;
            selectedRequest = r;
        };

        vm.OpenRequestCommand.Execute(request);

        eventRaised.Should().BeTrue();
        selectedRequest.Should().Be(request);
    }

    [Fact]
    public void OpenRequestCommand_WithNull_DoesNotRaiseEvent()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var eventRaised = false;
        vm.RequestSelected += (s, r) => eventRaised = true;

        vm.OpenRequestCommand.Execute(null);

        eventRaised.Should().BeFalse();
    }

    // Archive and Delete tests

    [Fact]
    public void IsConfirmingDelete_DefaultsToFalse()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.IsConfirmingDelete.Should().BeFalse();
    }

    [Fact]
    public void IsConfirmingDelete_CanBeSet()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.IsConfirmingDelete = true;
        vm.IsConfirmingDelete.Should().BeTrue();
    }

    [Fact]
    public void RequestToDelete_DefaultsToNull()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.RequestToDelete.Should().BeNull();
    }

    [Fact]
    public void RequestToDelete_CanBeSet()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var request = MockApiService.CreateTestRequest();
        vm.RequestToDelete = request;
        vm.RequestToDelete.Should().Be(request);
    }

    [Fact]
    public void RequestDeleteCommand_SetsIsConfirmingDeleteAndRequestToDelete()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var request = MockApiService.CreateTestRequest();

        vm.RequestDeleteCommand.Execute(request);

        vm.IsConfirmingDelete.Should().BeTrue();
        vm.RequestToDelete.Should().Be(request);
    }

    [Fact]
    public void RequestDeleteCommand_WithNull_DoesNothing()
    {
        var vm = _services.GetService<RequestsViewModel>();

        vm.RequestDeleteCommand.Execute(null);

        vm.IsConfirmingDelete.Should().BeFalse();
        vm.RequestToDelete.Should().BeNull();
    }

    [Fact]
    public void CancelDeleteCommand_ClearsState()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var request = MockApiService.CreateTestRequest();
        vm.RequestToDelete = request;
        vm.IsConfirmingDelete = true;

        vm.CancelDeleteCommand.Execute(null);

        vm.IsConfirmingDelete.Should().BeFalse();
        vm.RequestToDelete.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmDeleteCommand_DeletesRequest()
    {
        _services.MockApi.Setup(x => x.DeleteRequestAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var vm = _services.GetService<RequestsViewModel>();
        var request = MockApiService.CreateTestRequest();
        vm.Requests.Add(request);
        vm.RequestToDelete = request;
        vm.IsConfirmingDelete = true;

        vm.ConfirmDeleteCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.DeleteRequestAsync(request.Id), Times.Once);
        vm.Requests.Should().NotContain(request);
        vm.IsConfirmingDelete.Should().BeFalse();
        vm.RequestToDelete.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmDeleteCommand_WithNullRequestToDelete_DoesNothing()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.RequestToDelete = null;

        vm.ConfirmDeleteCommand.Execute(null);
        await Task.Delay(50);

        _services.MockApi.Verify(x => x.DeleteRequestAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmDeleteCommand_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.DeleteRequestAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cannot delete"));

        var vm = _services.GetService<RequestsViewModel>();
        var request = MockApiService.CreateTestRequest();
        vm.Requests.Add(request);
        vm.RequestToDelete = request;
        vm.IsConfirmingDelete = true;

        vm.ConfirmDeleteCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Cannot delete");
        vm.IsConfirmingDelete.Should().BeFalse();
        vm.RequestToDelete.Should().BeNull();
    }

    [Fact]
    public async Task ArchiveRequestCommand_ArchivesRequest()
    {
        var archivedRequest = MockApiService.CreateTestRequest();
        _services.MockApi.Setup(x => x.ArchiveRequestAsync(It.IsAny<string>()))
            .ReturnsAsync(archivedRequest);

        var vm = _services.GetService<RequestsViewModel>();
        var request = MockApiService.CreateTestRequest();
        vm.Requests.Add(request);

        vm.ArchiveRequestCommand.Execute(request);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.ArchiveRequestAsync(request.Id), Times.Once);
        vm.Requests.Should().NotContain(request);
    }

    [Fact]
    public async Task ArchiveRequestCommand_WithNull_DoesNothing()
    {
        var vm = _services.GetService<RequestsViewModel>();

        vm.ArchiveRequestCommand.Execute(null);
        await Task.Delay(50);

        _services.MockApi.Verify(x => x.ArchiveRequestAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ArchiveRequestCommand_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.ArchiveRequestAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cannot archive"));

        var vm = _services.GetService<RequestsViewModel>();
        var request = MockApiService.CreateTestRequest();
        vm.Requests.Add(request);

        vm.ArchiveRequestCommand.Execute(request);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Cannot archive");
    }

    [Fact]
    public void ArchiveRequestCommand_Exists()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.ArchiveRequestCommand.Should().NotBeNull();
    }

    [Fact]
    public void RequestDeleteCommand_Exists()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.RequestDeleteCommand.Should().NotBeNull();
    }

    [Fact]
    public void ConfirmDeleteCommand_Exists()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.ConfirmDeleteCommand.Should().NotBeNull();
    }

    [Fact]
    public void CancelDeleteCommand_Exists()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.CancelDeleteCommand.Should().NotBeNull();
    }

    [Fact]
    public void CreateRequestCommand_RaisesNewRequestRequestedEvent()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var eventRaised = false;
        vm.NewRequestRequested += (s, e) => eventRaised = true;

        vm.CreateRequestCommand.Execute(null);

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void NewRequestRequested_EventExists()
    {
        var vm = _services.GetService<RequestsViewModel>();
        vm.NewRequestRequested += (s, e) => { };
    }

    [Fact]
    public void AddRequest_InsertsRequestAtBeginning()
    {
        var vm = _services.GetService<RequestsViewModel>();
        var existingRequest = MockApiService.CreateTestRequest();
        existingRequest.Id = "existing";
        vm.Requests.Add(existingRequest);

        var newRequest = MockApiService.CreateTestRequest();
        newRequest.Id = "new";

        vm.AddRequest(newRequest);

        vm.Requests.Should().HaveCount(2);
        vm.Requests[0].Id.Should().Be("new");
    }

    [Fact]
    public void AddRequest_RaisesRequestSelectedEvent()
    {
        var vm = _services.GetService<RequestsViewModel>();
        RecordsRequest? selectedRequest = null;
        vm.RequestSelected += (s, r) => selectedRequest = r;

        var newRequest = MockApiService.CreateTestRequest();
        newRequest.Id = "new-req";

        vm.AddRequest(newRequest);

        selectedRequest.Should().NotBeNull();
        selectedRequest!.Id.Should().Be("new-req");
    }
}
