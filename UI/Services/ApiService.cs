﻿using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using UI.Controllers;
using UI.Models.DTOs;
using UI.Models.ViewModels;
using static UI.Services.SupMethods;

namespace UI.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private bool _isRefreshing = false;

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger,
            IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5003";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            _logger.LogInformation("ApiService initialized with BaseUrl: {BaseUrl}", baseUrl);
        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
                _logger.LogDebug("Authorization header set with JWT token");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            if (_isRefreshing) return false;

            _isRefreshing = true;
            try
            {
                var result = await RefreshTokenAsync();
                return result.Success;
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action) where T : class
        {
            SetAuthorizationHeader();
            var response = await action();

            // Check if response indicates unauthorized
            var apiResponse = response as dynamic;
            if (apiResponse?.Success == false && apiResponse?.Message?.Contains("Unauthorized") == true)
            {
                var refreshed = await TryRefreshTokenAsync();
                if (refreshed)
                {
                    SetAuthorizationHeader();
                    return await action();
                }
            }

            return response;
        }

        // Authentication methods
        public async Task<ApiResponse<UserResponse>> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/account/reg", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    // Save tokens
                    string token = result.token;
                    string refreshToken = result.refreshToken;
                    _httpContextAccessor.HttpContext?.Session.SetString("AccessToken", token);
                    _httpContextAccessor.HttpContext?.Session.SetString("RefreshToken", refreshToken);
                    _httpContextAccessor.HttpContext?.Session.SetString("TokenExpiry",
                        DateTime.UtcNow.AddMinutes(480).ToString("O"));

                    return new ApiResponse<UserResponse>
                    {
                        Success = true,
                        Data = new UserResponse
                        {
                            IsAuthenticated = true,
                            User = new UserInfo
                            {
                                Email = result.user.email,
                                FirstName = result.user.firstName,
                                LastName = result.user.lastName,
                                Id = result.user.id,
                                UserId = result.user.userId,
                                Roles = result.user.roles.ToObject<string[]>()
                            }
                        }
                    };
                }

                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = "Registration error"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new ApiResponse<UserResponse> { Success = false, Message = "Server connection error" };
            }
        }

        public async Task<ApiResponse<UserResponse>> LoginAsync(LoginViewModel model)
        {
            try
            {
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/account/log", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var dynamicResult = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    string token = dynamicResult.token;
                    string refreshToken = dynamicResult.refreshToken;
                    int expiresIn = dynamicResult.expiresIn;

                    _httpContextAccessor.HttpContext?.Session.SetString("AccessToken", token);
                    _httpContextAccessor.HttpContext?.Session.SetString("RefreshToken", refreshToken);
                    _httpContextAccessor.HttpContext?.Session.SetString("TokenExpiry",
                        DateTime.UtcNow.AddSeconds(expiresIn).ToString("O"));

                    var userInfo = new UserInfo
                    {
                        UserId = dynamicResult.user?.userId?.ToString(),
                        Id = dynamicResult.user?.id?.ToString(),
                        Email = dynamicResult.user?.email?.ToString(),
                        FirstName = dynamicResult.user?.firstName?.ToString(),
                        LastName = dynamicResult.user?.lastName?.ToString(),
                        Roles = dynamicResult.user?.roles?.ToObject<List<string>>() ?? new List<string>()
                    };

                    return new ApiResponse<UserResponse>
                    {
                        Success = true,
                        Data = new UserResponse { User = userInfo }
                    };
                }

                return new ApiResponse<UserResponse> { Success = false, Message = "Invalid email or password" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new ApiResponse<UserResponse> { Success = false, Message = "Server connection error" };
            }
        }

        public async Task<ApiResponse<object>> RefreshTokenAsync()
        {
            try
            {
                var token = _httpContextAccessor.HttpContext?.Session.GetString("AccessToken");
                var refreshToken = _httpContextAccessor.HttpContext?.Session.GetString("RefreshToken");

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
                {
                    return new ApiResponse<object> { Success = false, Message = "No tokens found" };
                }

                var request = new { Token = token, RefreshToken = refreshToken };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/account/refresh-token", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    string newToken = result.token;
                    string newRefreshToken = result.refreshToken;
                    int expiresIn = result.expiresIn;

                    _httpContextAccessor.HttpContext?.Session.SetString("AccessToken", newToken);
                    _httpContextAccessor.HttpContext?.Session.SetString("RefreshToken", newRefreshToken);
                    _httpContextAccessor.HttpContext?.Session.SetString("TokenExpiry",
                        DateTime.UtcNow.AddSeconds(expiresIn).ToString("O"));

                    return new ApiResponse<object> { Success = true };
                }

                return new ApiResponse<object> { Success = false, Message = "Token refresh failed" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new ApiResponse<object> { Success = false, Message = "Server connection error" };
            }
        }

        public async Task<ApiResponse<object>> LogoutAsync()
        {
            try
            {
                SetAuthorizationHeader();
                await _httpClient.PostAsync("/api/account/logout", null);

                _httpContextAccessor.HttpContext?.Session.Remove("AccessToken");
                _httpContextAccessor.HttpContext?.Session.Remove("RefreshToken");
                _httpContextAccessor.HttpContext?.Session.Remove("TokenExpiry");

                return new ApiResponse<object> { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return new ApiResponse<object> { Success = false };
            }
        }

        public async Task<ApiResponse<UserResponse>> CheckAuthStatusAsync()
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync("/api/account/me");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(content);

                    return new ApiResponse<UserResponse>
                    {
                        Success = true,
                        Data = new UserResponse
                        {
                            IsAuthenticated = true,
                            User = new UserInfo
                            {
                                Email = result.user.email,
                                FirstName = result.user.firstName,
                                LastName = result.user.lastName,
                                Roles = result.user.roles.ToObject<string[]>()
                            }
                        }
                    };
                }

                return new ApiResponse<UserResponse>
                {
                    Success = true,
                    Data = new UserResponse { IsAuthenticated = false }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking auth status");
                return new ApiResponse<UserResponse> { Success = false };
            }
        }

        // Book methods
        public async Task<ApiResponse<IEnumerable<BookDTO>>> GetAllBooksAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync("/api/books/getall");
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var books = JsonConvert.DeserializeObject<IEnumerable<BookDTO>>(responseContent);
                        return new ApiResponse<IEnumerable<BookDTO>> { Success = true, Data = books };
                    }

                    return new ApiResponse<IEnumerable<BookDTO>>
                    {
                        Success = false,
                        Message = $"Error: {response.StatusCode}"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting books");
                    return new ApiResponse<IEnumerable<BookDTO>> { Success = false, Message = "Error" };
                }
            });
        }

        public async Task<ApiResponse<IEnumerable<BookDTO>>> GetBooksWithFilteringAsync(
            string sortOrder = null, string searchString = null, string genre = null, string type = null)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var queryParams = new List<string>();
                    if (!string.IsNullOrEmpty(sortOrder)) queryParams.Add($"sortOrder={Uri.EscapeDataString(sortOrder)}");
                    if (!string.IsNullOrEmpty(searchString)) queryParams.Add($"searchString={Uri.EscapeDataString(searchString)}");
                    if (!string.IsNullOrEmpty(genre)) queryParams.Add($"genre={ConvertGenreToInt(genre)}");
                    if (!string.IsNullOrEmpty(type)) queryParams.Add($"type={ConvertBookTypeToInt(type)}");

                    var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                    var response = await _httpClient.GetAsync($"/api/books/filter{queryString}");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var books = JsonConvert.DeserializeObject<IEnumerable<BookDTO>>(content);
                        return new ApiResponse<IEnumerable<BookDTO>> { Success = true, Data = books };
                    }

                    return new ApiResponse<IEnumerable<BookDTO>> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error filtering books");
                    return new ApiResponse<IEnumerable<BookDTO>> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<BookDTO>> GetBookByIdAsync(int id)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync($"/api/books/getbyid/{id}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var book = JsonConvert.DeserializeObject<BookDTO>(content);
                        return new ApiResponse<BookDTO> { Success = true, Data = book };
                    }
                    return new ApiResponse<BookDTO> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting book {BookId}", id);
                    return new ApiResponse<BookDTO> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<BookDTO>> CreateBookAsync(BookDTO book)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var apiData = new
                    {
                        id = 0,
                        name = book.Title,
                        author = book.Author,
                        description = book.Description ?? "",
                        genre = ConvertGenreToNumber(book.Genre),
                        type = ConvertTypeToNumber(book.Type),
                        isAvailable = book.IsAvailable,
                        year = book.Year
                    };

                    var json = JsonConvert.SerializeObject(apiData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync("/api/books/createbook", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<BookDTO>(responseContent);
                        return new ApiResponse<BookDTO> { Success = true, Data = result };
                    }

                    return new ApiResponse<BookDTO> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating book");
                    return new ApiResponse<BookDTO> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> UpdateBookAsync(int id, BookDTO book)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var updateData = new
                    {
                        id = book.Id,
                        name = book.Title,
                        author = book.Author,
                        description = book.Description ?? "",
                        genre = ConvertGenreToInt(book.Genre),
                        type = ConvertBookTypeToInt(book.Type),
                        isAvailable = book.IsAvailable,
                        year = book.Year.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    };

                    var json = JsonConvert.SerializeObject(updateData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"/api/books/updatebook/{id}", content);

                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating book {BookId}", id);
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> DeleteBookAsync(int id)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.DeleteAsync($"/api/books/delete/{id}");
                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting book {BookId}", id);
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<IEnumerable<BookDTO>>> GetUserOrdersAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync("/api/books/getuserorders");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var books = JsonConvert.DeserializeObject<IEnumerable<BookDTO>>(content);
                        return new ApiResponse<IEnumerable<BookDTO>> { Success = true, Data = books };
                    }
                    return new ApiResponse<IEnumerable<BookDTO>> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting user orders");
                    return new ApiResponse<IEnumerable<BookDTO>> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<bool>> CheckBookAvailabilityAsync(int id)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync($"/api/books/availability/{id}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var isAvailable = JsonConvert.DeserializeObject<bool>(content);
                        return new ApiResponse<bool> { Success = true, Data = isAvailable };
                    }
                    return new ApiResponse<bool> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking availability");
                    return new ApiResponse<bool> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<bool>> SetBookAvailabilityAsync(int id, bool isAvailable)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var json = JsonConvert.SerializeObject(isAvailable);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"/api/books/setavailability/{id}", content);
                    return new ApiResponse<bool> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting availability");
                    return new ApiResponse<bool> { Success = false };
                }
            });
        }

        // Order methods
        public async Task<ApiResponse<IEnumerable<OrderDTO>>> GetAllOrdersAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync("/api/orders/getall");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var orders = JsonConvert.DeserializeObject<IEnumerable<OrderDTO>>(content);
                        return new ApiResponse<IEnumerable<OrderDTO>> { Success = true, Data = orders };
                    }
                    return new ApiResponse<IEnumerable<OrderDTO>> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting orders");
                    return new ApiResponse<IEnumerable<OrderDTO>> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<OrderDTO>> GetOrderByIdAsync(int id)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync($"/api/orders/findspecific/{id}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var order = JsonConvert.DeserializeObject<OrderDTO>(content);
                        return new ApiResponse<OrderDTO> { Success = true, Data = order };
                    }
                    return new ApiResponse<OrderDTO> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting order {OrderId}", id);
                    return new ApiResponse<OrderDTO> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> CreateOrderAsync(OrderDTO order)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var bookResult = await GetBookByIdAsync(order.BookId);
                    if (!bookResult.Success || bookResult.Data == null)
                        return new ApiResponse<object> { Success = false, Message = "Book not found" };

                    var orderData = new
                    {
                        id = 0,
                        userId = order.UserId,
                        bookId = order.BookId,
                        orderDate = order.OrderDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        type = 0,
                        book = new
                        {
                            id = bookResult.Data.Id,
                            name = bookResult.Data.Title,
                            author = bookResult.Data.Author,
                            description = bookResult.Data.Description ?? "",
                            genre = bookResult.Data.GenreId,
                            type = bookResult.Data.TypeId,
                            isAvailable = bookResult.Data.IsAvailable,
                            year = bookResult.Data.Year.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        }
                    };

                    var json = JsonConvert.SerializeObject(orderData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync("/api/orders/createnew", content);

                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating order");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> UpdateOrderAsync(int id, OrderDTO order)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var currentOrder = await GetOrderByIdAsync(id);
                    if (!currentOrder.Success) return new ApiResponse<object> { Success = false };

                    var json = JsonConvert.SerializeObject(order);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync("/api/orders/update", content);

                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating order");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> DeleteOrderAsync(int id)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.DeleteAsync($"/api/orders/delete/{id}");
                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting order");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> CancelOrderAsync(int orderId, string userId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var orderResult = await GetOrderByIdAsync(orderId);
                    if (!orderResult.Success) return new ApiResponse<object> { Success = false };

                    if (orderResult.Data.UserId != userId)
                        return new ApiResponse<object> { Success = false, Message = "Unauthorized" };

                    return await DeleteOrderAsync(orderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cancelling order");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<string>> GetUserEmailByIdAsync(string userId)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync($"/api/account/user/{userId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<dynamic>(content);
                        return new ApiResponse<string> { Success = true, Data = result.email };
                    }
                    return new ApiResponse<string> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting user email");
                    return new ApiResponse<string> { Success = false };
                }
            });
        }

        // Admin methods
        public async Task<ApiResponse<IEnumerable<UserDTO>>> GetAllUsersAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync("/api/users/getall");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<UserDTO>>>(content);
                        return apiResponse;
                    }
                    return new ApiResponse<IEnumerable<UserDTO>> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting users");
                    return new ApiResponse<IEnumerable<UserDTO>> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<UserDTO>> GetUserByIdAsync(string id)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync($"/api/users/getuserbyid?id={Uri.EscapeDataString(id)}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<UserDTO>>(content);
                        return apiResponse;
                    }
                    return new ApiResponse<UserDTO> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting user");
                    return new ApiResponse<UserDTO> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> CreateUserAsync(CreateUserRequest request)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync("/api/users/createuser", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        try
                        {
                            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                            return apiResponse ?? new ApiResponse<object> { Success = true };
                        }
                        catch
                        {
                            return new ApiResponse<object> { Success = true };
                        }
                    }

                    return new ApiResponse<object> { Success = false };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> CreateAdminAsync(CreateUserRequest request)
        {
            request.Role = "Administrator";
            return await CreateUserAsync(request);
        }

        public async Task<ApiResponse<object>> CreateManagerAsync(CreateUserRequest request)
        {
            request.Role = "Manager";
            return await CreateUserAsync(request);
        }

        public async Task<ApiResponse<object>> UpdateUserAsync(string id, UpdateUserRequest request)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"/api/users/updateuser?id={Uri.EscapeDataString(id)}", content);

                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> DeleteUserAsync(string id)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.DeleteAsync($"/api/users/deleteuser?id={Uri.EscapeDataString(id)}");
                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting user");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> ChangeUserPasswordAsync(string id, ChangePasswordRequest request)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"/api/users/changepassword?id={Uri.EscapeDataString(id)}", content);

                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error changing password");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> AssignRoleToUserAsync(string id, AssignRoleRequest request)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"/api/users/assignrole?id={Uri.EscapeDataString(id)}", content);

                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning role");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<object>> RemoveRoleFromUserAsync(string id, AssignRoleRequest request)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"/api/users/removerole?id={Uri.EscapeDataString(id)}", content);

                    return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing role");
                    return new ApiResponse<object> { Success = false };
                }
            });
        }

        public async Task<ApiResponse<IEnumerable<string>>> GetUserRolesAsync(string id)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync($"/api/users/getuserroles?id={Uri.EscapeDataString(id)}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<string>>>(content);
                        return apiResponse ?? new ApiResponse<IEnumerable<string>> { Success = true, Data = new List<string>() };
                    }
                    return new ApiResponse<IEnumerable<string>> { Success = false, Data = new List<string>() };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting user roles");
                    return new ApiResponse<IEnumerable<string>> { Success = false, Data = new List<string>() };
                }
            });
        }

        public async Task<ApiResponse<IEnumerable<RoleDTO>>> GetAllRolesAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync("/api/users/getallroles");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<RoleDTO>>>(content);
                        return apiResponse ?? new ApiResponse<IEnumerable<RoleDTO>> { Success = true, Data = new List<RoleDTO>() };
                    }
                    return new ApiResponse<IEnumerable<RoleDTO>> { Success = false, Data = new List<RoleDTO>() };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting all roles");
                    return new ApiResponse<IEnumerable<RoleDTO>> { Success = false, Data = new List<RoleDTO>() };
                }
            });
        }

        public async Task<ApiResponse<object>> RefreshSessionAsync()
        {
            // You can simply call the existing RefreshTokenAsync method
            return await RefreshTokenAsync();
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public Dictionary<string, IEnumerable<string>> ValidationErrors { get; set; } = new Dictionary<string, IEnumerable<string>>();
    }

    public class UserResponse
    {
        public bool IsAuthenticated { get; set; }
        public UserInfo User { get; set; }

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
    }

}