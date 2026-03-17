using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Tests.Mocks;
using Redact1.ViewModels;

namespace Redact1.Tests.ViewModels;

public class UsersViewModelTests : IDisposable
{
    private readonly TestServiceProvider _services;

    public UsersViewModelTests()
    {
        _services = new TestServiceProvider(isAuthenticated: true, isSupervisor: true);
        _services.SetupApp();
    }

    public void Dispose()
    {
        _services.Dispose();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var vm = _services.GetService<UsersViewModel>();

        vm.Users.Should().BeEmpty();
        vm.SelectedUser.Should().BeNull();
        vm.IsEditing.Should().BeFalse();
        vm.EditName.Should().BeEmpty();
        vm.EditEmail.Should().BeEmpty();
        vm.EditRole.Should().Be("clerk");
        vm.EditPassword.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadUsersAsync_LoadsUsers()
    {
        var users = new List<User>
        {
            MockApiService.CreateTestUser(),
            MockApiService.CreateTestUser(true)
        };
        _services.MockApi.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(users);

        var vm = _services.GetService<UsersViewModel>();
        await vm.LoadUsersAsync();

        vm.Users.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadUsersAsync_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.GetUsersAsync())
            .ThrowsAsync(new Exception("Access denied"));

        var vm = _services.GetService<UsersViewModel>();
        await vm.LoadUsersAsync();

        vm.ErrorMessage.Should().Contain("Access denied");
    }

    [Fact]
    public void StartCreateUserCommand_SetsEditingMode()
    {
        var vm = _services.GetService<UsersViewModel>();

        vm.StartCreateUserCommand.Execute(null);

        vm.IsEditing.Should().BeTrue();
        vm.SelectedUser.Should().BeNull();
        vm.EditName.Should().BeEmpty();
        vm.EditEmail.Should().BeEmpty();
        vm.EditRole.Should().Be("clerk");
    }

    [Fact]
    public void StartEditUserCommand_SetsEditingModeWithUser()
    {
        var vm = _services.GetService<UsersViewModel>();
        var user = MockApiService.CreateTestUser();

        vm.StartEditUserCommand.Execute(user);

        vm.IsEditing.Should().BeTrue();
        vm.SelectedUser.Should().Be(user);
        vm.EditName.Should().Be(user.Name);
        vm.EditEmail.Should().Be(user.Email);
        vm.EditRole.Should().Be(user.Role);
    }

    [Fact]
    public void CancelEditCommand_ClearsEditingMode()
    {
        var vm = _services.GetService<UsersViewModel>();
        vm.StartCreateUserCommand.Execute(null);

        vm.CancelEditCommand.Execute(null);

        vm.IsEditing.Should().BeFalse();
        vm.SelectedUser.Should().BeNull();
    }

    [Fact]
    public async Task SaveUserCommand_CreateUser_WithEmptyName_SetsError()
    {
        var vm = _services.GetService<UsersViewModel>();
        vm.StartCreateUserCommand.Execute(null);
        vm.EditName = "";
        vm.EditEmail = "test@test.com";

        vm.SaveUserCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task SaveUserCommand_CreateUser_WithEmptyPassword_SetsError()
    {
        var vm = _services.GetService<UsersViewModel>();
        vm.StartCreateUserCommand.Execute(null);
        vm.EditName = "Test User";
        vm.EditEmail = "test@test.com";
        vm.EditPassword = "";

        vm.SaveUserCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Password");
    }

    [Fact]
    public async Task SaveUserCommand_CreateUser_Success()
    {
        var newUser = new User { Id = "new-user", Name = "New User", Email = "new@test.com", Role = "clerk" };
        _services.MockApi.Setup(x => x.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(newUser);

        var vm = _services.GetService<UsersViewModel>();
        vm.StartCreateUserCommand.Execute(null);
        vm.EditName = "New User";
        vm.EditEmail = "new@test.com";
        vm.EditRole = "clerk";
        vm.EditPassword = "password123";

        vm.SaveUserCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.CreateUserAsync(It.Is<CreateUserRequest>(r =>
            r.Name == "New User" &&
            r.Email == "new@test.com" &&
            r.Role == "clerk" &&
            r.Password == "password123"
        )), Times.Once);
        vm.Users.Should().Contain(newUser);
        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public async Task SaveUserCommand_UpdateUser_Success()
    {
        var existingUser = MockApiService.CreateTestUser();
        var updatedUser = new User { Id = existingUser.Id, Name = "Updated Name", Email = existingUser.Email, Role = "supervisor" };

        _services.MockApi.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<User> { existingUser });
        _services.MockApi.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(updatedUser);

        var vm = _services.GetService<UsersViewModel>();
        await vm.LoadUsersAsync();
        vm.StartEditUserCommand.Execute(existingUser);
        vm.EditName = "Updated Name";
        vm.EditRole = "supervisor";

        vm.SaveUserCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateUserAsync(existingUser.Id, It.Is<UpdateUserRequest>(r =>
            r.Name == "Updated Name" && r.Role == "supervisor"
        )), Times.Once);
        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmDeleteCommand_DeletesUser()
    {
        var user = MockApiService.CreateTestUser();
        _services.MockApi.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<User> { user });

        var vm = _services.GetService<UsersViewModel>();
        await vm.LoadUsersAsync();
        vm.RequestDeleteUserCommand.Execute(user);

        vm.ConfirmDeleteCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.DeleteUserAsync(user.Id), Times.Once);
        vm.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task StartEditUserCommand_WithNullUser_DoesNothing()
    {
        var vm = _services.GetService<UsersViewModel>();

        vm.StartEditUserCommand.Execute(null);
        await Task.Delay(50);

        vm.IsEditing.Should().BeFalse();
        vm.SelectedUser.Should().BeNull();
    }

    [Fact]
    public async Task RequestDeleteUserCommand_WithNullUser_DoesNothing()
    {
        var vm = _services.GetService<UsersViewModel>();

        vm.RequestDeleteUserCommand.Execute(null);
        await Task.Delay(50);

        vm.IsConfirmingDelete.Should().BeFalse();
        vm.UserToDelete.Should().BeNull();
    }

    [Fact]
    public async Task SaveUserCommand_UpdateUser_WithPassword_UpdatesPassword()
    {
        var existingUser = MockApiService.CreateTestUser();
        var updatedUser = new User { Id = existingUser.Id, Name = "Test", Email = existingUser.Email, Role = "clerk" };

        _services.MockApi.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<User> { existingUser });
        _services.MockApi.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(updatedUser);

        var vm = _services.GetService<UsersViewModel>();
        await vm.LoadUsersAsync();
        vm.StartEditUserCommand.Execute(existingUser);
        vm.EditPassword = "newpassword123";

        vm.SaveUserCommand.Execute(null);
        await Task.Delay(100);

        _services.MockApi.Verify(x => x.UpdateUserAsync(existingUser.Id, It.Is<UpdateUserRequest>(r =>
            r.Password == "newpassword123"
        )), Times.Once);
    }

    [Fact]
    public async Task SaveUserCommand_OnError_SetsErrorMessage()
    {
        _services.MockApi.Setup(x => x.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ThrowsAsync(new Exception("Failed to create user"));

        var vm = _services.GetService<UsersViewModel>();
        vm.StartCreateUserCommand.Execute(null);
        vm.EditName = "New User";
        vm.EditEmail = "new@test.com";
        vm.EditPassword = "password123";

        vm.SaveUserCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Failed to create user");
    }

    [Fact]
    public async Task ConfirmDeleteCommand_OnError_SetsErrorMessage()
    {
        var user = MockApiService.CreateTestUser();
        _services.MockApi.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<User> { user });
        _services.MockApi.Setup(x => x.DeleteUserAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cannot delete user"));

        var vm = _services.GetService<UsersViewModel>();
        await vm.LoadUsersAsync();
        vm.RequestDeleteUserCommand.Execute(user);

        vm.ConfirmDeleteCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("Cannot delete user");
    }

    [Fact]
    public async Task SaveUserCommand_WithEmptyEmail_SetsError()
    {
        var vm = _services.GetService<UsersViewModel>();
        vm.StartCreateUserCommand.Execute(null);
        vm.EditName = "Test User";
        vm.EditEmail = "";

        vm.SaveUserCommand.Execute(null);
        await Task.Delay(100);

        vm.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task SaveUserCommand_UpdateUser_IndexNotFound_StillCompletes()
    {
        var existingUser = MockApiService.CreateTestUser();
        var updatedUser = new User { Id = existingUser.Id, Name = "Updated", Email = existingUser.Email, Role = "clerk" };

        _services.MockApi.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserRequest>()))
            .ReturnsAsync(updatedUser);

        var vm = _services.GetService<UsersViewModel>();
        // Don't load users, so the user won't be in the collection
        vm.SelectedUser = existingUser;
        vm.IsEditing = true;
        vm.EditName = "Updated";
        vm.EditEmail = existingUser.Email;
        vm.EditRole = "clerk";

        vm.SaveUserCommand.Execute(null);
        await Task.Delay(100);

        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void LoadUsersCommand_Exists()
    {
        var vm = _services.GetService<UsersViewModel>();

        vm.LoadUsersCommand.Should().NotBeNull();
    }

    [Fact]
    public void Users_CanBeSet()
    {
        var vm = _services.GetService<UsersViewModel>();
        var users = new System.Collections.ObjectModel.ObservableCollection<User>();

        vm.Users = users;

        vm.Users.Should().BeSameAs(users);
    }

    [Fact]
    public void SelectedUser_CanBeSet()
    {
        var vm = _services.GetService<UsersViewModel>();
        var user = MockApiService.CreateTestUser();

        vm.SelectedUser = user;

        vm.SelectedUser.Should().Be(user);
    }

    [Fact]
    public void EditName_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<UsersViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.EditName))
                propertyChanged = true;
        };

        vm.EditName = "New Name";

        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void EditEmail_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<UsersViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.EditEmail))
                propertyChanged = true;
        };

        vm.EditEmail = "new@email.com";

        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void EditRole_PropertyChanged_RaisesEvent()
    {
        var vm = _services.GetService<UsersViewModel>();
        var propertyChanged = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.EditRole))
                propertyChanged = true;
        };

        vm.EditRole = "supervisor";

        propertyChanged.Should().BeTrue();
    }

    [Fact]
    public void EditPassword_CanBeSet()
    {
        var vm = _services.GetService<UsersViewModel>();

        vm.EditPassword = "newpassword";

        vm.EditPassword.Should().Be("newpassword");
    }
}
