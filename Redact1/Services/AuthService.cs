using Redact1.Models;

namespace Redact1.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApiService _apiService;
        private readonly IStorageService _storageService;

        public User? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;
        public bool IsEnrolled => _storageService.GetAgencyConfig() != null;
        public AgencyConfig? CurrentAgency => _storageService.GetAgencyConfig();
        public event EventHandler<User?>? AuthStateChanged;

        public AuthService(IApiService apiService, IStorageService storageService)
        {
            _apiService = apiService;
            _storageService = storageService;
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            var token = _storageService.GetAuthToken();
            var user = _storageService.GetUser();

            if (string.IsNullOrEmpty(token) || user == null)
            {
                return false;
            }

            _apiService.SetAuthToken(token);

            try
            {
                // Validate token by fetching current user
                var currentUser = await _apiService.GetCurrentUserAsync();
                CurrentUser = currentUser;
                _storageService.SetUser(currentUser);
                AuthStateChanged?.Invoke(this, currentUser);
                return true;
            }
            catch
            {
                // Token is invalid, clear storage
                _storageService.ClearAuthToken();
                _storageService.ClearUser();
                _apiService.SetAuthToken(null);
                return false;
            }
        }

        public async Task<User> LoginAsync(string emailOrEmployeeId, string password, bool useEmployeeId = false)
        {
            var request = new LoginRequest
            {
                Password = password
            };

            if (useEmployeeId)
            {
                request.EmployeeId = emailOrEmployeeId;
            }
            else
            {
                request.Email = emailOrEmployeeId;
            }

            var response = await _apiService.LoginAsync(request);

            _storageService.SetAuthToken(response.Token);
            _storageService.SetUser(response.User);
            _apiService.SetAuthToken(response.Token);

            CurrentUser = response.User;
            AuthStateChanged?.Invoke(this, response.User);

            return response.User;
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _apiService.LogoutAsync();
            }
            catch
            {
                // Ignore logout errors
            }

            _storageService.ClearAuthToken();
            _storageService.ClearUser();
            _apiService.SetAuthToken(null);

            CurrentUser = null;
            AuthStateChanged?.Invoke(this, null);
        }

        public void SetDepartmentCode(string code)
        {
            // For demo purposes, create a simple agency config
            // In production, this would fetch config from a discovery service
            var config = new AgencyConfig
            {
                Code = code,
                Name = code == "DEMO" ? "Demo Police Department" : $"{code} Police Department",
                ApiBaseUrl = "https://redact-1-worker.joelstevick.workers.dev",
                LoginIdentifiers = new List<string> { "email" }
            };

            _storageService.SetAgencyConfig(config);
        }

        public void ClearEnrollment()
        {
            _storageService.ClearAgencyConfig();
            _storageService.ClearAuthToken();
            _storageService.ClearUser();
            _apiService.SetAuthToken(null);

            CurrentUser = null;
            AuthStateChanged?.Invoke(this, null);
        }
    }
}
