using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.Enums;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class UserManagementViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly CurrentSessionContext _sessionContext;
    private List<UserDto> _allUsers = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _roleFilters = new() { "Tất cả", "Staff", "Manager", "Admin" };

    [ObservableProperty]
    private string _selectedRoleFilter = "Tất cả";

    [ObservableProperty]
    private ObservableCollection<string> _statusFilters = new() { "Tất cả", "Active", "Locked", "Inactive" };

    [ObservableProperty]
    private string _selectedStatusFilter = "Tất cả";

    [ObservableProperty]
    private ObservableCollection<string> _roleOptions = new() { "Staff", "Manager", "Admin" };

    // Modal popup state
    [ObservableProperty]
    private bool _isAddPopupOpen;

    [ObservableProperty]
    private bool _isEditPopupOpen;

    [ObservableProperty]
    private bool _isResetPasswordPopupOpen;

    // Selected user for editing
    [ObservableProperty]
    private UserDto? _selectedUser;

    // Add User Inputs
    [ObservableProperty]
    private string _inputUsername = string.Empty;

    [ObservableProperty]
    private string _inputPassword = string.Empty;

    [ObservableProperty]
    private string _inputFullName = string.Empty;

    [ObservableProperty]
    private string _inputEmail = string.Empty;

    [ObservableProperty]
    private string _inputPhone = string.Empty;

    [ObservableProperty]
    private string _inputRole = "Staff";

    // Edit User Inputs
    [ObservableProperty]
    private string _editFullName = string.Empty;

    [ObservableProperty]
    private string _editEmail = string.Empty;

    [ObservableProperty]
    private string _editPhone = string.Empty;

    [ObservableProperty]
    private string _editRole = "Staff";

    // Reset Password Input
    [ObservableProperty]
    private string _newPasswordInput = string.Empty;

    public ObservableCollection<UserDto> Users { get; } = new();

    public UserManagementViewModel(IAuthService authService, CurrentSessionContext sessionContext)
    {
        _authService = authService;
        _sessionContext = sessionContext;
        
        // Load data on startup
        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var requestingUserId = _sessionContext.CurrentUser?.UserId ?? Guid.Empty;
            var usersList = await _authService.GetAllUsersAsync(requestingUserId);
            
            _allUsers = usersList.ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tải danh sách người dùng: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        var filtered = _allUsers.AsEnumerable();

        // Filter by Search Term (Username, FullName, Email)
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();
            filtered = filtered.Where(u => 
                (u.Username != null && u.Username.ToLower().Contains(term)) ||
                (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term))
            );
        }

        // Filter by Role
        if (SelectedRoleFilter != "Tất cả")
        {
            filtered = filtered.Where(u => string.Equals(u.Role, SelectedRoleFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by Status
        if (SelectedStatusFilter != "Tất cả")
        {
            filtered = filtered.Where(u => string.Equals(u.Status, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
        }

        Users.Clear();
        foreach (var u in filtered)
        {
            Users.Add(u);
        }
    }

    // Add User Flow
    [RelayCommand]
    private void OpenAddPopup()
    {
        InputUsername = string.Empty;
        InputPassword = string.Empty;
        InputFullName = string.Empty;
        InputEmail = string.Empty;
        InputPhone = string.Empty;
        InputRole = "Staff";
        
        IsAddPopupOpen = true;
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        if (string.IsNullOrWhiteSpace(InputUsername) || string.IsNullOrWhiteSpace(InputPassword) || string.IsNullOrWhiteSpace(InputFullName))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ Tên đăng nhập, Mật khẩu và Họ tên", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsLoading = true;
            var requestingUserId = _sessionContext.CurrentUser?.UserId ?? Guid.Empty;
            
            var dto = new CreateUserDto(
                InputUsername.Trim(),
                InputPassword,
                InputFullName.Trim(),
                InputRole,
                InputEmail.Trim(),
                string.IsNullOrWhiteSpace(InputPhone) ? null : InputPhone.Trim()
            );

            await _authService.CreateUserAsync(dto, requestingUserId);
            IsAddPopupOpen = false;
            
            MessageBox.Show("Tạo người dùng mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tạo người dùng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Edit User Flow
    [RelayCommand]
    private void OpenEditPopup(UserDto user)
    {
        SelectedUser = user;
        EditFullName = user.FullName;
        EditEmail = user.Email;
        EditPhone = user.PhoneNumber ?? string.Empty;
        EditRole = user.Role;
        
        IsEditPopupOpen = true;
    }

    [RelayCommand]
    private async Task UpdateUserAsync()
    {
        if (SelectedUser == null) return;

        if (string.IsNullOrWhiteSpace(EditFullName))
        {
            MessageBox.Show("Họ tên không được để trống", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsLoading = true;
            var requestingUserId = _sessionContext.CurrentUser?.UserId ?? Guid.Empty;

            var dto = new UpdateUserDto
            {
                Id = SelectedUser.Id,
                FullName = EditFullName.Trim(),
                Email = EditEmail.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim(),
                Role = EditRole
            };

            await _authService.UpdateUserAsync(dto, requestingUserId);
            IsEditPopupOpen = false;
            
            MessageBox.Show("Cập nhật thông tin thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Toggle status Flow (Lock / Active)
    [RelayCommand]
    private async Task ToggleUserStatusAsync(Guid userId)
    {
        try
        {
            IsLoading = true;
            var requestingUserId = _sessionContext.CurrentUser?.UserId ?? Guid.Empty;
            await _authService.ToggleUserStatusAsync(userId, requestingUserId);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi thay đổi trạng thái: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Reset Password Flow
    [RelayCommand]
    private void OpenResetPasswordPopup(UserDto user)
    {
        SelectedUser = user;
        NewPasswordInput = string.Empty;
        IsResetPasswordPopupOpen = true;
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (SelectedUser == null) return;

        if (string.IsNullOrWhiteSpace(NewPasswordInput))
        {
            MessageBox.Show("Mật khẩu mới không được để trống", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsLoading = true;
            var requestingUserId = _sessionContext.CurrentUser?.UserId ?? Guid.Empty;
            await _authService.ResetPasswordAsync(SelectedUser.Id, NewPasswordInput, requestingUserId);
            IsResetPasswordPopupOpen = false;
            
            MessageBox.Show("Đặt lại mật khẩu thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi đặt lại mật khẩu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClosePopups()
    {
        IsAddPopupOpen = false;
        IsEditPopupOpen = false;
        IsResetPasswordPopupOpen = false;
        SelectedUser = null;
    }
}
