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

        public ObservableCollection<RecordsRequest> Requests { get; } = new();

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

        public event EventHandler<RecordsRequest>? RequestSelected;

        public RequestsViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();
            CreateRequestCommand = new AsyncRelayCommand(CreateRequestAsync);
            OpenRequestCommand = new RelayCommand<RecordsRequest>(OpenRequest);
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

        private async Task CreateRequestAsync()
        {
            var requestNumber = $"FOIA-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            var payload = new CreateRequestPayload
            {
                RequestNumber = requestNumber,
                Title = "New Request",
                RequestDate = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };

            try
            {
                var request = await _apiService.CreateRequestAsync(payload);
                Requests.Insert(0, request);
                RequestSelected?.Invoke(this, request);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
        }

        private void OpenRequest(RecordsRequest? request)
        {
            if (request != null)
                RequestSelected?.Invoke(this, request);
        }
    }
}
