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

        // Books
        Task<ApiResponse<IEnumerable<BookDTO>>> GetAllBooksAsync();
        Task<ApiResponse<IEnumerable<BookDTO>>> GetBooksWithFilteringAsync(string sortOrder = null, string searchString = null, string genre = null, string type = null);
        Task<ApiResponse<BookDTO>> GetBookByIdAsync(int id);
        Task<ApiResponse<BookDTO>> CreateBookAsync(BookDTO book);
        Task<ApiResponse<object>> UpdateBookAsync(int id, BookDTO book);
        Task<ApiResponse<object>> DeleteBookAsync(int id);
        Task<ApiResponse<IEnumerable<BookDTO>>> GetUserOrdersAsync();
        Task<ApiResponse<bool>> CheckBookAvailabilityAsync(int id);
        Task<ApiResponse<object>> CancelOrderAsync(int orderId, string userId);

        // Orders
        Task<ApiResponse<IEnumerable<OrderDTO>>> GetAllOrdersAsync();
        Task<ApiResponse<OrderDTO>> GetOrderByIdAsync(int id);
        Task<ApiResponse<object>> CreateOrderAsync(OrderDTO order);
        Task<ApiResponse<object>> UpdateOrderAsync(int id, OrderDTO order);
        Task<ApiResponse<object>> DeleteOrderAsync(int id);
        Task<ApiResponse<string>> GetUserEmailByIdAsync(string userId);
    }

}
