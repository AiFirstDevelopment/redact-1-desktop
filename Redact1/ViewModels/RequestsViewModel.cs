using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Redact1.ViewModels
{
    public class RequestsViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private string _searchText = string.Empty;
        private string _statusFilter = "all";
        private bool _showArchived;
        private bool _isConfirmingDelete;
        private RecordsRequest? _requestToDelete;

        public ObservableCollection<RecordsRequest> Requests { get; } = new();

        public bool IsConfirmingDelete
        {
            get => _isConfirmingDelete;
            set => SetProperty(ref _isConfirmingDelete, value);
        }

        public RecordsRequest? RequestToDelete
        {
            get => _requestToDelete;
            set => SetProperty(ref _requestToDelete, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    _ = LoadRequestsAsync();
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (SetProperty(ref _statusFilter, value))
                    _ = LoadRequestsAsync();
            }
        }

        public bool ShowArchived
        {
            get => _showArchived;
            set => SetProperty(ref _showArchived, value);
        }

        public ICommand CreateRequestCommand { get; }
        public ICommand OpenRequestCommand { get; }
        public ICommand ArchiveRequestCommand { get; }
        public ICommand RequestDeleteCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }

        public event EventHandler<RecordsRequest>? RequestSelected;
        public event EventHandler? NewRequestRequested;

        public RequestsViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();
            CreateRequestCommand = new AsyncRelayCommand(CreateRequestAsync);
            OpenRequestCommand = new RelayCommand<RecordsRequest>(OpenRequest);
            ArchiveRequestCommand = new AsyncRelayCommand<RecordsRequest>(ArchiveRequestAsync);
            RequestDeleteCommand = new RelayCommand<RecordsRequest>(RequestDelete);
            ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
            CancelDeleteCommand = new RelayCommand(CancelDelete);
        }

        public async Task LoadRequestsAsync()
        {
            IsLoading = true;
            ClearError();

            try
            {
                List<RecordsRequest> requests;

                if (ShowArchived)
                {
                    requests = await _apiService.GetArchivedRequestsAsync(
                        string.IsNullOrWhiteSpace(SearchText) ? null : SearchText
                    );
                }
                else
                {
                    var status = StatusFilter == "all" ? null : StatusFilter;
                    requests = await _apiService.GetRequestsAsync(
                        status,
                        string.IsNullOrWhiteSpace(SearchText) ? null : SearchText
                    );
                }

                Requests.Clear();
                foreach (var request in requests)
                {
                    Requests.Add(request);
                }
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

        private Task CreateRequestAsync()
        {
            NewRequestRequested?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public void AddRequest(RecordsRequest request)
        {
            Requests.Insert(0, request);
            RequestSelected?.Invoke(this, request);
        }

        private void OpenRequest(RecordsRequest? request)
        {
            if (request != null)
                RequestSelected?.Invoke(this, request);
        }

        private async Task ArchiveRequestAsync(RecordsRequest? request)
        {
            if (request == null) return;

            try
            {
                await _apiService.ArchiveRequestAsync(request.Id);
                Requests.Remove(request);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private void RequestDelete(RecordsRequest? request)
        {
            if (request == null) return;
            RequestToDelete = request;
            IsConfirmingDelete = true;
        }

        private async Task ConfirmDeleteAsync()
        {
            if (RequestToDelete == null) return;

            try
            {
                await _apiService.DeleteRequestAsync(RequestToDelete.Id);
                Requests.Remove(RequestToDelete);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsConfirmingDelete = false;
                RequestToDelete = null;
            }
        }

        private void CancelDelete()
        {
            IsConfirmingDelete = false;
            RequestToDelete = null;
        }
    }
}
