using Microsoft.Extensions.DependencyInjection;
using Redact1.Models;
using Redact1.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Redact1.ViewModels
{
    public class UsersViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        private ObservableCollection<User> _users = new();
        private User? _selectedUser;
        private User? _userToDelete;
        private bool _isEditing;
        private bool _isConfirmingDelete;
        private string _editName = string.Empty;
        private string _editEmail = string.Empty;
        private string _editRole = "clerk";
        private string _editPassword = string.Empty;

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool IsConfirmingDelete
        {
            get => _isConfirmingDelete;
            set => SetProperty(ref _isConfirmingDelete, value);
        }

        public User? UserToDelete
        {
            get => _userToDelete;
            set => SetProperty(ref _userToDelete, value);
        }

        public string EditName
        {
            get => _editName;
            set => SetProperty(ref _editName, value);
        }

        public string EditEmail
        {
            get => _editEmail;
            set => SetProperty(ref _editEmail, value);
        }

        public string EditRole
        {
            get => _editRole;
            set => SetProperty(ref _editRole, value);
        }

        public string EditPassword
        {
            get => _editPassword;
            set => SetProperty(ref _editPassword, value);
        }

        public ICommand LoadUsersCommand { get; }
        public ICommand StartCreateUserCommand { get; }
        public ICommand StartEditUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand RequestDeleteUserCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }

        public UsersViewModel()
        {
            _apiService = App.Services.GetRequiredService<IApiService>();

            LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
            StartCreateUserCommand = new RelayCommand(StartCreateUser);
            StartEditUserCommand = new RelayCommand<User>(StartEditUser);
            SaveUserCommand = new AsyncRelayCommand(SaveUserAsync);
            CancelEditCommand = new RelayCommand(CancelEdit);
            RequestDeleteUserCommand = new RelayCommand<User>(RequestDeleteUser);
            ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
            CancelDeleteCommand = new RelayCommand(CancelDelete);
        }

        public async Task LoadUsersAsync()
        {
            IsLoading = true;
            ClearError();

            try
            {
                var users = await _apiService.GetUsersAsync();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
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

        private void StartCreateUser()
        {
            SelectedUser = null;
            EditName = string.Empty;
            EditEmail = string.Empty;
            EditRole = "clerk";
            EditPassword = string.Empty;
            IsEditing = true;
        }

        private void StartEditUser(User? user)
        {
            if (user == null) return;
            SelectedUser = user;
            EditName = user.Name;
            EditEmail = user.Email;
            EditRole = user.Role;
            EditPassword = string.Empty;
            IsEditing = true;
        }

        private async Task SaveUserAsync()
        {
            if (string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditEmail))
            {
                SetError("Name and email are required");
                return;
            }

            IsLoading = true;

            try
            {
                if (SelectedUser == null)
                {
                    if (string.IsNullOrWhiteSpace(EditPassword))
                    {
                        SetError("Password is required for new users");
                        return;
                    }

                    var request = new CreateUserRequest
                    {
                        Name = EditName,
                        Email = EditEmail,
                        Role = EditRole,
                        Password = EditPassword
                    };

                    var user = await _apiService.CreateUserAsync(request);
                    Users.Add(user);
                }
                else
                {
                    var request = new UpdateUserRequest
                    {
                        Name = EditName,
                        Email = EditEmail,
                        Role = EditRole
                    };

                    if (!string.IsNullOrWhiteSpace(EditPassword))
                    {
                        request.Password = EditPassword;
                    }

                    var user = await _apiService.UpdateUserAsync(SelectedUser.Id, request);
                    var index = Users.IndexOf(SelectedUser);
                    if (index >= 0)
                    {
                        Users[index] = user;
                    }
                }

                IsEditing = false;
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

        private void CancelEdit()
        {
            IsEditing = false;
            SelectedUser = null;
        }

        private void RequestDeleteUser(User? user)
        {
            if (user == null) return;
            UserToDelete = user;
            IsConfirmingDelete = true;
        }

        private async Task ConfirmDeleteAsync()
        {
            if (UserToDelete == null) return;

            try
            {
                await _apiService.DeleteUserAsync(UserToDelete.Id);
                Users.Remove(UserToDelete);
            }
            catch (Exception ex)
            {
                SetError(ex);
            }
            finally
            {
                IsConfirmingDelete = false;
                UserToDelete = null;
            }
        }

        private void CancelDelete()
        {
            IsConfirmingDelete = false;
            UserToDelete = null;
        }
    }
}
