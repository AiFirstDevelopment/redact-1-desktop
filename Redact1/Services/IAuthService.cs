using Redact1.Models;

namespace Redact1.Services
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        bool IsAuthenticated { get; }
        bool IsEnrolled { get; }
        AgencyConfig? CurrentAgency { get; }
        event EventHandler<User?>? AuthStateChanged;

        Task<bool> TryRestoreSessionAsync();
        Task<User> LoginAsync(string emailOrEmployeeId, string password, bool useEmployeeId = false);
        Task LogoutAsync();
        void SetDepartmentCode(string code);
        void ClearEnrollment();
    }
}
