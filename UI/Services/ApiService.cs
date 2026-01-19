using Newtonsoft.Json;
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

        private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> httpAction)
        {
            SetAuthorizationHeader();
            var response = await httpAction();

            // Check if response indicates unauthorized (401) and try to refresh
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await TryRefreshTokenAsync();
                if (refreshed)
                {
                    SetAuthorizationHeader();
                    response = await httpAction();
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

                var errorResult = JsonConvert.DeserializeObject<dynamic>(responseContent);
                string message = null;

                if (errorResult?.errors != null)
                {
                    var errors = (Newtonsoft.Json.Linq.JObject)errorResult.errors;

                    var firstError = errors.Properties().FirstOrDefault();

                    if (firstError != null && firstError.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                    {
                        message = arr[0].ToString();
                    }
                }

                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = message ?? "Помилка реєстрації"
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

                // Clear the authorization header after logout
                _httpClient.DefaultRequestHeaders.Authorization = null;

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

                // Try to refresh token if unauthorized
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var refreshed = await TryRefreshTokenAsync();
                    if (refreshed)
                    {
                        SetAuthorizationHeader();
                        response = await _httpClient.GetAsync("/api/account/me");
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(content);

                    // Handle both nested and flat response structures
                    var userData = result.user ?? result;

                    return new ApiResponse<UserResponse>
                    {
                        Success = true,
                        Data = new UserResponse
                        {
                            IsAuthenticated = true,
                            User = new UserInfo
                            {
                                Id = userData.id?.ToString(),
                                UserId = userData.userId?.ToString(),
                                Email = userData.email?.ToString(),
                                FirstName = userData.firstName?.ToString(),
                                LastName = userData.lastName?.ToString(),
                                Roles = userData.roles?.ToObject<List<string>>() ?? new List<string>()
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
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync("/api/books/getall"));
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
        }

        public async Task<ApiResponse<IEnumerable<BookDTO>>> GetBooksWithFilteringAsync(
            string sortOrder = null, string searchString = null, string genre = null, string type = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(sortOrder)) queryParams.Add($"sortOrder={Uri.EscapeDataString(sortOrder)}");
                if (!string.IsNullOrEmpty(searchString)) queryParams.Add($"searchString={Uri.EscapeDataString(searchString)}");
                if (!string.IsNullOrEmpty(genre)) queryParams.Add($"genre={ConvertGenreToInt(genre)}");
                if (!string.IsNullOrEmpty(type)) queryParams.Add($"type={ConvertBookTypeToInt(type)}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync($"/api/books/filter{queryString}"));

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
        }

        public async Task<ApiResponse<BookDTO>> GetBookByIdAsync(int id)
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync($"/api/books/getbyid/{id}"));
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
        }

        public async Task<ApiResponse<BookDTO>> CreateBookAsync(BookDTO book)
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
                var response = await SendWithRetryAsync(() => _httpClient.PostAsync("/api/books/createbook", content));

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
        }

        public async Task<ApiResponse<object>> UpdateBookAsync(int id, BookDTO book)
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
                var response = await SendWithRetryAsync(() => _httpClient.PutAsync($"/api/books/updatebook/{id}", content));

                return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book {BookId}", id);
                return new ApiResponse<object> { Success = false };
            }
        }

        public async Task<ApiResponse<object>> DeleteBookAsync(int id)
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.DeleteAsync($"/api/books/delete/{id}"));
                return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book {BookId}", id);
                return new ApiResponse<object> { Success = false };
            }
        }

        public async Task<ApiResponse<IEnumerable<BookDTO>>> GetUserOrdersAsync()
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync("/api/books/getuserorders"));
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
        }

        public async Task<ApiResponse<bool>> CheckBookAvailabilityAsync(int id)
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync($"/api/books/availability/{id}"));
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
        }

        public async Task<ApiResponse<bool>> SetBookAvailabilityAsync(int id, bool isAvailable)
        {
            try
            {
                var json = JsonConvert.SerializeObject(isAvailable);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithRetryAsync(() => _httpClient.PutAsync($"/api/books/setavailability/{id}", content));
                return new ApiResponse<bool> { Success = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting availability");
                return new ApiResponse<bool> { Success = false };
            }
        }

        // Order methods
        public async Task<ApiResponse<IEnumerable<OrderDTO>>> GetAllOrdersAsync()
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync("/api/orders/getall"));
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
        }

        public async Task<ApiResponse<OrderDTO>> GetOrderByIdAsync(int id)
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync($"/api/orders/findspecific/{id}"));
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
        }

        public async Task<ApiResponse<object>> CreateOrderAsync(OrderDTO order)
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
                    type = 1,
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
                var response = await SendWithRetryAsync(() => _httpClient.PostAsync("/api/orders/createnew", content));

                return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return new ApiResponse<object> { Success = false };
            }
        }

        public async Task<ApiResponse<object>> UpdateOrderAsync(int id, OrderDTO order)
        {
            try
            {
                _logger.LogInformation("UpdateOrderAsync called with id={Id}, order={@Order}", id, order);
                
                var currentOrder = await GetOrderByIdAsync(id);
                if (!currentOrder.Success)
                {
                    _logger.LogWarning("GetOrderByIdAsync failed for id={Id}", id);
                    return new ApiResponse<object> { Success = false, Message = "Order not found" };
                }

                // Получаем данные книги для отправки в API
                var bookResult = await GetBookByIdAsync(order.BookId);
                if (!bookResult.Success || bookResult.Data == null)
                {
                    return new ApiResponse<object> { Success = false, Message = "Book not found" };
                }

                var orderData = new
                {
                    id = order.Id,
                    userId = order.UserId,
                    bookId = order.BookId,
                    orderDate = order.OrderDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    type = order.Type,
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
                _logger.LogInformation("Sending PUT request to /api/orders/update with body: {Json}", json);
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithRetryAsync(() => _httpClient.PutAsync("/api/orders/update", content));

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API response: StatusCode={StatusCode}, Content={Content}", 
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = false, Message = $"API Error: {response.StatusCode} - {responseContent}" };
                }

                return new ApiResponse<object> { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                return new ApiResponse<object> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<object>> DeleteOrderAsync(int id)
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.DeleteAsync($"/api/orders/delete/{id}"));
                return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order");
                return new ApiResponse<object> { Success = false };
            }
        }

        public async Task<ApiResponse<object>> CancelOrderAsync(int orderId, string userId)
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
        }

        public async Task<ApiResponse<string>> GetUserEmailByIdAsync(string userId)
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync($"/api/account/user/{userId}"));
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
        }

        // Admin methods
        public async Task<ApiResponse<IEnumerable<UserDTO>>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("=== GetAllUsersAsync called ===");
                _logger.LogInformation("Base URL: {BaseUrl}", _httpClient.BaseAddress);
                
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync("/api/users/getall"));
                
                _logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response Content: {Content}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<UserDTO>>>(content);
                    _logger.LogInformation("Parsed users count: {Count}", apiResponse?.Data?.Count() ?? 0);
                    return apiResponse;
                }
                
                _logger.LogWarning("GetAllUsersAsync failed with status {StatusCode}", response.StatusCode);
                return new ApiResponse<IEnumerable<UserDTO>> { Success = false, Message = $"HTTP {response.StatusCode}: {content}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return new ApiResponse<IEnumerable<UserDTO>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<UserDTO>> GetUserByIdAsync(string id)
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync($"/api/users/getuserbyid?id={Uri.EscapeDataString(id)}"));
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
        }

        public async Task<ApiResponse<object>> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithRetryAsync(() => _httpClient.PostAsync("/api/users/createuser", content));

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("CreateUser response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
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

                // Parse error response to get detailed error messages
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return errorResponse ?? new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = $"Error: {response.StatusCode}" 
                    };
                }
                catch
                {
                    return new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = $"Error: {response.StatusCode} - {responseContent}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return new ApiResponse<object> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<object>> CreateAdminAsync(CreateUserRequest request)
        {
            try
            {
                request.Role = "Administrator";
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithRetryAsync(() => _httpClient.PostAsync("/api/users/createadmin", content));

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("CreateAdmin response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
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

                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return errorResponse ?? new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = $"Error: {response.StatusCode}" 
                    };
                }
                catch
                {
                    return new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = $"Error: {response.StatusCode} - {responseContent}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin");
                return new ApiResponse<object> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<object>> CreateManagerAsync(CreateUserRequest request)
        {
            try
            {
                request.Role = "Manager";
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithRetryAsync(() => _httpClient.PostAsync("/api/users/createmanager", content));

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("CreateManager response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
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

                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return errorResponse ?? new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = $"Error: {response.StatusCode}" 
                    };
                }
                catch
                {
                    return new ApiResponse<object> 
                    { 
                        Success = false, 
                        Message = $"Error: {response.StatusCode} - {responseContent}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manager");
                return new ApiResponse<object> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<object>> UpdateUserAsync(string id, UpdateUserRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithRetryAsync(() => _httpClient.PutAsync($"/api/users/updateuser?id={Uri.EscapeDataString(id)}", content));

                return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return new ApiResponse<object> { Success = false };
            }
        }

        public async Task<ApiResponse<object>> DeleteUserAsync(string id)
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.DeleteAsync($"/api/users/deleteuser?id={Uri.EscapeDataString(id)}"));
                return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return new ApiResponse<object> { Success = false };
            }
        }

        public async Task<ApiResponse<object>> ChangeUserPasswordAsync(string id, ChangePasswordRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithRetryAsync(() => _httpClient.PostAsync($"/api/users/changepassword?id={Uri.EscapeDataString(id)}", content));

                return new ApiResponse<object> { Success = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return new ApiResponse<object> { Success = false };
            }
        }

        public async Task<ApiResponse<object>> AssignRoleToUserAsync(string id, AssignRoleRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithRetryAsync(() => _httpClient.PostAsync("/api/users/assignrole", content));

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("AssignRole response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true, Message = "Role assigned successfully" };
                }

                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return errorResponse ?? new ApiResponse<object> { Success = false, Message = $"Error: {response.StatusCode}" };
                }
                catch
                {
                    return new ApiResponse<object> { Success = false, Message = $"Error: {response.StatusCode} - {responseContent}" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role");
                return new ApiResponse<object> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<object>> RemoveRoleFromUserAsync(string id, AssignRoleRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await SendWithRetryAsync(() => _httpClient.PostAsync("/api/users/removerole", content));

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("RemoveRole response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true, Message = "Role removed successfully" };
                }

                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return errorResponse ?? new ApiResponse<object> { Success = false, Message = $"Error: {response.StatusCode}" };
                }
                catch
                {
                    return new ApiResponse<object> { Success = false, Message = $"Error: {response.StatusCode} - {responseContent}" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role");
                return new ApiResponse<object> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResponse<IEnumerable<string>>> GetUserRolesAsync(string id)
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync($"/api/users/getuserroles?id={Uri.EscapeDataString(id)}"));
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
        }

        public async Task<ApiResponse<IEnumerable<RoleDTO>>> GetAllRolesAsync()
        {
            try
            {
                var response = await SendWithRetryAsync(() => _httpClient.GetAsync("/api/users/getallroles"));
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