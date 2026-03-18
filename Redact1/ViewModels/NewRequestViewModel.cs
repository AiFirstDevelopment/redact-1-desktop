using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;

namespace Redact1.ViewModels
{
    public class NewRequestViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        private string _requestNumber = string.Empty;
        private string _title = string.Empty;
        private DateTimeOffset _requestDate = DateTimeOffset.Now;
        private string _notes = string.Empty;

        public string RequestNumber
        {
            get => _requestNumber;
            set
            {
                if (SetProperty(ref _requestNumber, value))
                    OnPropertyChanged(nameof(CanCreate));
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                    OnPropertyChanged(nameof(CanCreate));
            }
        }

        public DateTimeOffset RequestDate
        {
            get => _requestDate;
            set => SetProperty(ref _requestDate, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public bool CanCreate => !string.IsNullOrWhiteSpace(RequestNumber) && !string.IsNullOrWhiteSpace(Title);

        public NewRequestViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();
            GenerateRequestNumber();
        }

        public void GenerateRequestNumber()
        {
            RequestNumber = $"RR-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}";
        }

        public async Task<RecordsRequest?> CreateRequestAsync()
        {
            if (!CanCreate) return null;

            IsLoading = true;
            ClearError();

            var payload = new CreateRequestPayload
            {
                RequestNumber = RequestNumber,
                Title = Title,
                RequestDate = RequestDate.ToUnixTimeMilliseconds(),
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes
            };

            try
            {
                var request = await _apiService.CreateRequestAsync(payload);
                return request;
            }
            catch (Exception ex)
            {
                SetError(ex);
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
