using UI.Models.DTOs;
using UI.Models.ViewModels;

namespace UI.Services
{
    public interface IApiService
    {
        // Authentication
        Task<ApiResponse<UserResponse>> RegisterAsync(RegisterViewModel model);
        Task<ApiResponse<UserResponse>> LoginAsync(LoginViewModel model);
        Task<ApiResponse<object>> LogoutAsync();
        Task<ApiResponse<UserResponse>> CheckAuthStatusAsync();
        Task<ApiResponse<object>> RefreshSessionAsync();

        // Books
        Task<ApiResponse<IEnumerable<Models.DTOs.BookDTO>>> GetAllBooksAsync();
        Task<ApiResponse<IEnumerable<Models.DTOs.BookDTO>>> GetBooksWithFilteringAsync(string sortOrder = null, string searchString = null, string genre = null, string type = null);
        Task<ApiResponse<Models.DTOs.BookDTO>> GetBookByIdAsync(int id);
        Task<ApiResponse<Models.DTOs.BookDTO>> CreateBookAsync(Models.DTOs.BookDTO book);
        Task<ApiResponse<object>> UpdateBookAsync(int id, Models.DTOs.BookDTO book);
        Task<ApiResponse<object>> DeleteBookAsync(int id);
        Task<ApiResponse<IEnumerable<Models.DTOs.BookDTO>>> GetUserOrdersAsync();
        Task<ApiResponse<bool>> CheckBookAvailabilityAsync(int id);
        Task<ApiResponse<bool>> SetBookAvailabilityAsync(int id, bool isAvailable);
        Task<ApiResponse<object>> CancelOrderAsync(int orderId, string userId);

        // Orders
        Task<ApiResponse<IEnumerable<Models.DTOs.OrderDTO>>> GetAllOrdersAsync();
        Task<ApiResponse<Models.DTOs.OrderDTO>> GetOrderByIdAsync(int id);
        Task<ApiResponse<object>> CreateOrderAsync(Models.DTOs.OrderDTO order);
        Task<ApiResponse<object>> UpdateOrderAsync(int id, Models.DTOs.OrderDTO order);
        Task<ApiResponse<object>> DeleteOrderAsync(int id);
        Task<ApiResponse<string>> GetUserEmailByIdAsync(string userId);

        // Admin
        Task<ApiResponse<IEnumerable<Models.DTOs.UserDTO>>> GetAllUsersAsync();
        Task<ApiResponse<Models.DTOs.UserDTO>> GetUserByIdAsync(string id);
        Task<ApiResponse<object>> CreateUserAsync(Models.DTOs.CreateUserRequest request);
        Task<ApiResponse<object>> CreateAdminAsync(Models.DTOs.CreateUserRequest request);
        Task<ApiResponse<object>> CreateManagerAsync(Models.DTOs.CreateUserRequest request);
        Task<ApiResponse<object>> UpdateUserAsync(string id, Models.DTOs.UpdateUserRequest request);
        Task<ApiResponse<object>> DeleteUserAsync(string id);
        Task<ApiResponse<object>> ChangeUserPasswordAsync(string id, Models.DTOs.ChangePasswordRequest request);
        Task<ApiResponse<object>> AssignRoleToUserAsync(string id, Models.DTOs.AssignRoleRequest request);
        Task<ApiResponse<object>> RemoveRoleFromUserAsync(string id, Models.DTOs.AssignRoleRequest request);
        Task<ApiResponse<IEnumerable<string>>> GetUserRolesAsync(string id);
        Task<ApiResponse<IEnumerable<Models.DTOs.RoleDTO>>> GetAllRolesAsync();

        Task<ApiResponse<object>> RefreshTokenAsync();
    }

}
