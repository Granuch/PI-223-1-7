using Newtonsoft.Json;
using System.Net;
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

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;

            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            var baseUrl = _configuration["ApiSettings:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = "https://localhost:5003";
                _logger.LogWarning("ApiSettings:BaseUrl не знайдено в конфігурації. Використовується значення за замовчуванням: {BaseUrl}", baseUrl);
            }

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            _logger.LogInformation("ApiService ініціалізовано з BaseUrl: {BaseUrl}", baseUrl);
        }

        private void EnsureCookiesAreSet()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Request?.Cookies != null && httpContext.Request.Cookies.Count > 0)
                {
                    if (_httpClient.DefaultRequestHeaders.Contains("Cookie"))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("Cookie");
                    }

                    var cookieHeader = string.Join("; ",
                        httpContext.Request.Cookies.Select(c => $"{c.Key}={c.Value}"));

                    _httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);

                    _logger.LogDebug("Set cookies for API request. Count: {Count}", httpContext.Request.Cookies.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cookies in HTTP client");
            }
        }

        private void LogCookieInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request?.Cookies != null)
            {
                _logger.LogInformation("Current cookies count: {Count}", httpContext.Request.Cookies.Count);
                foreach (var cookie in httpContext.Request.Cookies)
                {
                    _logger.LogInformation("Cookie: {Name} = {Value}", cookie.Key,
                        cookie.Value.Length > 50 ? cookie.Value.Substring(0, 50) + "..." : cookie.Value);
                }
            }
        }

        private ApiResponse<T> HandleAuthError<T>(HttpStatusCode statusCode, string defaultMessage)
        {
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized response - clearing session");
                _httpContextAccessor.HttpContext?.Session.Clear();
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = "Необхідно увійти в систему"
                };
            }

            if (statusCode == HttpStatusCode.Forbidden)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = "Недостатньо прав доступу"
                };
            }

            return new ApiResponse<T>
            {
                Success = false,
                Message = $"{defaultMessage}: {statusCode}"
            };
        }

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

                var errorResult = JsonConvert.DeserializeObject<dynamic>(responseContent);
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = errorResult.message ?? "Помилка реєстрації"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<UserResponse>> LoginAsync(LoginViewModel model)
        {
            try
            {
                _logger.LogInformation("Attempting login for user: {Email}", model.Email);

                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/account/log", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var dynamicResult = JsonConvert.DeserializeObject<dynamic>(responseContent);

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

                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = "Невірний email або пароль"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> LogoutAsync()
        {
            try
            {
                EnsureCookiesAreSet();
                var response = await _httpClient.PostAsync("/api/account/logout", null);

                return new ApiResponse<object>
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Вихід успішний" : "Помилка виходу"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<UserResponse>> CheckAuthStatusAsync()
        {
            try
            {
                EnsureCookiesAreSet();
                var response = await _httpClient.GetAsync("/api/account/stat");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    if (result.isAuthenticated == true)
                    {
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
                    else
                    {
                        return new ApiResponse<UserResponse>
                        {
                            Success = true,
                            Data = new UserResponse { IsAuthenticated = false }
                        };
                    }
                }

                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = "Помилка перевірки статусу"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking auth status");
                return new ApiResponse<UserResponse>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> RefreshSessionAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing session");
                EnsureCookiesAreSet();

                var userDataJson = _httpContextAccessor.HttpContext?.Session.GetString("UserData");
                if (string.IsNullOrEmpty(userDataJson))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Дані користувача не знайдені в сесії"
                    };
                }

                var userData = JsonConvert.DeserializeObject<UserInfo>(userDataJson);
                var request = new RefreshSessionRequest { Email = userData.Email };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/account/checkandrefresh", content);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка оновлення сесії");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing session");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<BookDTO>>> GetAllBooksAsync()
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.GetAsync("/api/books/getall");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var books = JsonConvert.DeserializeObject<IEnumerable<BookDTO>>(responseContent);
                    return new ApiResponse<IEnumerable<BookDTO>>
                    {
                        Success = true,
                        Data = books
                    };
                }

                return HandleAuthError<IEnumerable<BookDTO>>(response.StatusCode, "Помилка отримання книг");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all books");
                return new ApiResponse<IEnumerable<BookDTO>>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<BookDTO>>> GetBooksWithFilteringAsync(string sortOrder = null, string searchString = null, string genre = null, string type = null)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(sortOrder))
                    queryParams.Add($"sortOrder={Uri.EscapeDataString(sortOrder)}");
                if (!string.IsNullOrEmpty(searchString))
                    queryParams.Add($"searchString={Uri.EscapeDataString(searchString)}");
                if (!string.IsNullOrEmpty(genre))
                {
                    int genreId = ConvertGenreToInt(genre);
                    queryParams.Add($"genre={genreId}");
                }
                if (!string.IsNullOrEmpty(type))
                {
                    int typeId = ConvertBookTypeToInt(type);
                    queryParams.Add($"type={typeId}");
                }

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var fullUrl = $"/api/books/filter{queryString}";

                var response = await _httpClient.GetAsync(fullUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var books = JsonConvert.DeserializeObject<IEnumerable<BookDTO>>(responseContent);
                    return new ApiResponse<IEnumerable<BookDTO>>
                    {
                        Success = true,
                        Data = books
                    };
                }

                return HandleAuthError<IEnumerable<BookDTO>>(response.StatusCode, "Помилка отримання книг");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered books");
                return new ApiResponse<IEnumerable<BookDTO>>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<BookDTO>> GetBookByIdAsync(int id)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.GetAsync($"/api/books/getbyid/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var book = JsonConvert.DeserializeObject<BookDTO>(responseContent);
                    return new ApiResponse<BookDTO>
                    {
                        Success = true,
                        Data = book
                    };
                }

                return HandleAuthError<BookDTO>(response.StatusCode, "Книга не знайдена");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting book by id: {id}");
                return new ApiResponse<BookDTO>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<BookDTO>> CreateBookAsync(BookDTO book)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                int genreValue = ConvertGenreToNumber(book.Genre);
                int typeValue = ConvertTypeToNumber(book.Type);

                var apiBookData = new
                {
                    id = 0,
                    name = book.Title,
                    author = book.Author,
                    description = book.Description ?? "",
                    genre = genreValue,
                    type = typeValue,
                    isAvailable = book.IsAvailable,
                    year = book.Year
                };

                var json = JsonConvert.SerializeObject(apiBookData, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/books/createbook", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    var createdBook = new BookDTO
                    {
                        Id = apiResponse.id,
                        Title = apiResponse.name,
                        Author = apiResponse.author,
                        Description = apiResponse.description,
                        GenreId = apiResponse.genre,
                        TypeId = apiResponse.type,
                        IsAvailable = apiResponse.isAvailable,
                        Year = apiResponse.year,
                        OrderId = null
                    };

                    return new ApiResponse<BookDTO>
                    {
                        Success = true,
                        Data = createdBook
                    };
                }

                return HandleAuthError<BookDTO>(response.StatusCode, "Помилка створення книги");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                return new ApiResponse<BookDTO>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> UpdateBookAsync(int id, BookDTO book)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                int genreValue = ConvertGenreToInt(book.Genre);
                int typeValue = ConvertBookTypeToInt(book.Type);

                var updateData = new
                {
                    id = book.Id,
                    name = book.Title,
                    author = book.Author,
                    description = book.Description ?? "",
                    genre = genreValue,
                    type = typeValue,
                    isAvailable = book.IsAvailable,
                    year = book.Year.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var json = JsonConvert.SerializeObject(updateData, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/api/books/updatebook/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Книга успішно оновлена"
                    };
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка оновлення книги");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book: {BookId}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером: " + ex.Message
                };
            }
        }

        public async Task<ApiResponse<object>> DeleteBookAsync(int id)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.DeleteAsync($"/api/books/delete/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Книга видалена"
                    };
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка видалення книги");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting book: {id}");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<BookDTO>>> GetUserOrdersAsync()
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.GetAsync("/api/books/getuserorders");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var books = JsonConvert.DeserializeObject<IEnumerable<BookDTO>>(responseContent);
                    return new ApiResponse<IEnumerable<BookDTO>>
                    {
                        Success = true,
                        Data = books
                    };
                }

                return HandleAuthError<IEnumerable<BookDTO>>(response.StatusCode, "Помилка отримання замовлень");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders");
                return new ApiResponse<IEnumerable<BookDTO>>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<bool>> CheckBookAvailabilityAsync(int id)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.GetAsync($"/api/books/availability/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var isAvailable = JsonConvert.DeserializeObject<bool>(responseContent);
                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Data = isAvailable
                    };
                }

                return HandleAuthError<bool>(response.StatusCode, "Помилка перевірки доступності");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking book availability: {id}");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<bool>> SetBookAvailabilityAsync(int id, bool isAvailable)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var json = JsonConvert.SerializeObject(isAvailable);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/api/books/setavailability/{id}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<bool>
                    {
                        Success = true,
                    };
                }

                return HandleAuthError<bool>(response.StatusCode, "Помилка перевірки доступності");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting book availability: {id}");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<OrderDTO>>> GetAllOrdersAsync()
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.GetAsync("/api/orders/getall");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var orders = JsonConvert.DeserializeObject<IEnumerable<OrderDTO>>(responseContent);
                    return new ApiResponse<IEnumerable<OrderDTO>>
                    {
                        Success = true,
                        Data = orders
                    };
                }

                return HandleAuthError<IEnumerable<OrderDTO>>(response.StatusCode, "Помилка отримання замовлень");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all orders");
                return new ApiResponse<IEnumerable<OrderDTO>>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<OrderDTO>> GetOrderByIdAsync(int id)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.GetAsync($"/api/orders/findspecific/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var order = JsonConvert.DeserializeObject<OrderDTO>(responseContent);
                    return new ApiResponse<OrderDTO>
                    {
                        Success = true,
                        Data = order
                    };
                }

                return HandleAuthError<OrderDTO>(response.StatusCode, "Замовлення не знайдено");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting order by id: {id}");
                return new ApiResponse<OrderDTO>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> CreateOrderAsync(OrderDTO order)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var bookResult = await GetBookByIdAsync(order.BookId);
                if (!bookResult.Success || bookResult.Data == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Не вдалося отримати інформацію про книгу"
                    };
                }

                if (!bookResult.Data.IsAvailable)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Книга вже недоступна для замовлення"
                    };
                }

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
                        name = "Test",
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

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Замовлення успішно створено"
                    };
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка створення замовлення");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> UpdateOrderAsync(int id, OrderDTO order)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var currentOrderResult = await GetOrderByIdAsync(id);
                if (!currentOrderResult.Success || currentOrderResult.Data?.Book == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Помилка отримання поточних даних замовлення"
                    };
                }

                var currentBook = currentOrderResult.Data.Book;

                if (order.BookId != currentBook.Id)
                {
                    var newBookResult = await GetBookByIdAsync(order.BookId);
                    if (newBookResult.Success && newBookResult.Data != null)
                    {
                        currentBook = newBookResult.Data;
                    }
                }

                var updateData = new
                {
                    id = order.Id,
                    userId = order.UserId,
                    bookId = order.BookId,
                    orderDate = order.OrderDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    type = order.Type,
                    book = new
                    {
                        id = currentBook.Id,
                        name = currentBook.Title ?? "",
                        author = currentBook.Author ?? "",
                        description = currentBook.Description ?? "",
                        genre = ConvertGenreToInt(currentBook.Genre),
                        type = ConvertTypeToInt(currentBook.Type),
                        isAvailable = currentBook.IsAvailable,
                        year = currentBook.Year.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                };

                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"/api/orders/update", content);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Замовлення оновлено"
                    };
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка оновлення замовлення");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order: {OrderId}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> DeleteOrderAsync(int id)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.DeleteAsync($"/api/orders/delete/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Замовлення видалено"
                    };
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка видалення замовлення");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting order: {id}");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> CancelOrderAsync(int orderId, string userId)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var orderResult = await GetOrderByIdAsync(orderId);
                if (!orderResult.Success || orderResult.Data == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Замовлення не знайдено"
                    };
                }

                if (orderResult.Data.UserId != userId)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Ви не маєте права скасувати це замовлення"
                    };
                }

                if (orderResult.Data.Type != 1)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Можна скасувати тільки активні замовлення"
                    };
                }

                var response = await _httpClient.DeleteAsync($"/api/orders/delete/{orderId}");

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Замовлення успішно скасовано"
                    };
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка скасування замовлення");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<string>> GetUserEmailByIdAsync(string userId)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.GetAsync($"api/account/user/{userId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<string>
                    {
                        Success = true,
                        Data = result.email
                    };
                }

                return HandleAuthError<string>(response.StatusCode, "Користувач не знайдений");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user email by id: {userId}");
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<UserDTO>>> GetAllUsersAsync()
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                _logger.LogInformation("Getting all users");

                var response = await _httpClient.GetAsync("/api/users/getall");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("API Response Content length: {ContentLength}", responseContent.Length);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<UserDTO>>>(responseContent);
                    return apiResponse;
                }

                return HandleAuthError<IEnumerable<UserDTO>>(response.StatusCode, "Помилка отримання користувачів");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new ApiResponse<IEnumerable<UserDTO>>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<UserDTO>> GetUserByIdAsync(string id)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.GetAsync($"/api/users/getuserbyid?id={Uri.EscapeDataString(id)}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<UserDTO>>(responseContent);
                    return apiResponse;
                }

                return HandleAuthError<UserDTO>(response.StatusCode, "Помилка отримання користувача");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return new ApiResponse<UserDTO>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                if (string.IsNullOrEmpty(request.Role))
                {
                    request.Role = "RegisteredUser";
                }

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/users/createuser", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                        return apiResponse ?? new ApiResponse<object> { Success = true };
                    }
                    catch (JsonSerializationException)
                    {
                        return new ApiResponse<object> { Success = true, Message = "Користувач створений успішно" };
                    }
                }

                return await ProcessCreateUserErrorResponse(responseContent, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", request.Email);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> CreateAdminAsync(CreateUserRequest request)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                request.Role = "Administrator";

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/users/createadmin", content);

                return await ProcessCreateUserResponse(response, "адміністратора");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin: {Email}", request.Email);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> CreateManagerAsync(CreateUserRequest request)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                request.Role = "Manager";

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/users/createmanager", content);

                return await ProcessCreateUserResponse(response, "менеджера");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manager: {Email}", request.Email);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> UpdateUserAsync(string id, UpdateUserRequest request)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"/api/users/updateuser?id={Uri.EscapeDataString(id)}", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return apiResponse;
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка оновлення користувача");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> DeleteUserAsync(string id)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var response = await _httpClient.DeleteAsync($"/api/users/deleteuser?id={Uri.EscapeDataString(id)}");

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return apiResponse;
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка видалення користувача");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> ChangeUserPasswordAsync(string id, ChangePasswordRequest request)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"/api/users/changepassword?id={Uri.EscapeDataString(id)}", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                        return apiResponse ?? new ApiResponse<object> { Success = true, Message = "Пароль змінений успішно" };
                    }
                    catch (JsonException)
                    {
                        return new ApiResponse<object> { Success = true, Message = "Пароль змінений успішно" };
                    }
                }

                return HandleAuthError<object>(response.StatusCode, "Помилка зміни паролю");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<object>> AssignRoleToUserAsync(string id, AssignRoleRequest request)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                request.UserId = id;

                _logger.LogInformation("Assigning role {RoleName} to user: {UserId}", request.RoleName, id);

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/api/users/assignrole?id={Uri.EscapeDataString(id)}", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("API Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                        return apiResponse ?? new ApiResponse<object> { Success = true, Message = "Роль призначена успішно" };
                    }
                    catch (JsonException)
                    {
                        return new ApiResponse<object> { Success = true, Message = "Роль призначена успішно" };
                    }
                }

                return await HandleErrorResponse(responseContent, response.StatusCode, "Помилка призначення ролі");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleName} to user: {UserId}", request?.RoleName, id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Помилка з'єднання з сервером: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<object>> RemoveRoleFromUserAsync(string id, AssignRoleRequest request)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                request.UserId = id;

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/api/users/removerole?id={Uri.EscapeDataString(id)}", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                        return apiResponse ?? new ApiResponse<object> { Success = true, Message = "Роль видалена успішно" };
                    }
                    catch (JsonException)
                    {
                        return new ApiResponse<object> { Success = true, Message = "Роль видалена успішно" };
                    }
                }

                return await HandleErrorResponse(responseContent, response.StatusCode, "Помилка видалення ролі");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleName} from user: {UserId}", request?.RoleName, id);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Помилка з'єднання з сервером: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<string>>> GetUserRolesAsync(string id)
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                _logger.LogInformation("Getting roles for user: {UserId}", id);

                var response = await _httpClient.GetAsync($"/api/users/getuserroles?id={Uri.EscapeDataString(id)}");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("GetUserRoles API response: Status={StatusCode}, Content={Content}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<string>>>(responseContent);
                        if (apiResponse != null && apiResponse.Success)
                        {
                            return apiResponse;
                        }

                        var directRoles = JsonConvert.DeserializeObject<IEnumerable<string>>(responseContent);
                        if (directRoles != null)
                        {
                            return new ApiResponse<IEnumerable<string>>
                            {
                                Success = true,
                                Data = directRoles,
                                Message = "Роли получены успешно"
                            };
                        }

                        _logger.LogWarning("Could not deserialize user roles response: {Content}", responseContent);
                        return new ApiResponse<IEnumerable<string>>
                        {
                            Success = true,
                            Data = new List<string>(),
                            Message = "Роли не найдены"
                        };
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "JSON deserialization failed for user roles. Content: {Content}", responseContent);

                        return new ApiResponse<IEnumerable<string>>
                        {
                            Success = true,
                            Data = new List<string>(),
                            Message = "Ошибка обработки данных ролей"
                        };
                    }
                }

                return HandleAuthError<IEnumerable<string>>(response.StatusCode, "Ошибка получения ролей пользователя");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles: {UserId}", id);
                return new ApiResponse<IEnumerable<string>>
                {
                    Success = false,
                    Message = "Ошибка соединения с сервером",
                    Data = new List<string>()
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<RoleDTO>>> GetAllRolesAsync()
        {
            try
            {
                LogCookieInfo();
                EnsureCookiesAreSet();

                _logger.LogInformation("Getting all roles");

                var response = await _httpClient.GetAsync("/api/users/getallroles");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("GetAllRoles API response: Status={StatusCode}, Content={Content}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<RoleDTO>>>(responseContent);
                        if (apiResponse != null)
                        {
                            return apiResponse;
                        }

                        var directRoles = JsonConvert.DeserializeObject<IEnumerable<RoleDTO>>(responseContent);
                        return new ApiResponse<IEnumerable<RoleDTO>>
                        {
                            Success = true,
                            Data = directRoles ?? new List<RoleDTO>(),
                            Message = "Роли получены успешно"
                        };
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "JSON deserialization failed for roles. Content: {Content}", responseContent);
                        return new ApiResponse<IEnumerable<RoleDTO>>
                        {
                            Success = false,
                            Message = "Ошибка обработки данных ролей",
                            Data = new List<RoleDTO>()
                        };
                    }
                }

                return HandleAuthError<IEnumerable<RoleDTO>>(response.StatusCode, "Ошибка получения ролей");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                return new ApiResponse<IEnumerable<RoleDTO>>
                {
                    Success = false,
                    Message = "Ошибка соединения с сервером",
                    Data = new List<RoleDTO>()
                };
            }
        }

        private async Task<ApiResponse<object>> ProcessCreateUserResponse(HttpResponseMessage response, string userType)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return apiResponse ?? new ApiResponse<object>
                    {
                        Success = true,
                        Message = $"{userType} створений успішно"
                    };
                }
                catch (JsonSerializationException)
                {
                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = $"{userType} створений успішно"
                    };
                }
            }

            return await ProcessCreateUserErrorResponse(responseContent, response.StatusCode);
        }

        private async Task<ApiResponse<object>> ProcessCreateUserErrorResponse(string responseContent, HttpStatusCode statusCode)
        {
            if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
            {
                return HandleAuthError<object>(statusCode, "Ошибка создания пользователя");
            }

            try
            {
                if (string.IsNullOrEmpty(responseContent))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Ошибка создания пользователя: {statusCode}"
                    };
                }

                var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                var apiResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Ошибка создания пользователя"
                };

                if (errorResponse?.errors != null)
                {
                    var errorsList = new List<string>();
                    foreach (var error in errorResponse.errors)
                    {
                        if (error.Value != null)
                        {
                            foreach (var message in error.Value)
                            {
                                errorsList.Add($"{error.Name}: {message}");
                            }
                        }
                    }
                    if (errorsList.Any())
                    {
                        apiResponse.Errors = errorsList;
                        apiResponse.Message = "Ошибки валидации: " + string.Join("; ", errorsList);
                    }
                }
                else if (errorResponse?.message != null)
                {
                    apiResponse.Message = errorResponse.message.ToString();
                }

                return apiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing error response: {Content}", responseContent);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Ошибка создания пользователя: {statusCode}"
                };
            }
        }

        private async Task<ApiResponse<object>> HandleErrorResponse(string responseContent, HttpStatusCode statusCode, string defaultErrorMessage)
        {
            if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
            {
                return HandleAuthError<object>(statusCode, defaultErrorMessage);
            }

            try
            {
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                if (apiResponse != null)
                {
                    return apiResponse;
                }
            }
            catch (JsonException)
            {
                try
                {
                    var validationError = JsonConvert.DeserializeObject<ValidationErrorResponse>(responseContent);
                    if (validationError?.Errors != null)
                    {
                        var errorMessages = validationError.Errors
                            .SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}: {string.Join(", ", v)}"))
                            .ToList();

                        return new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Помилки валідації: {string.Join("; ", errorMessages)}"
                        };
                    }
                }
                catch (JsonException){}
            }

            return new ApiResponse<object>
            {
                Success = false,
                Message = $"{defaultErrorMessage}: {statusCode}"
            };
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
    }

    public class RefreshSessionRequest
    {
        public string Email { get; set; }
    }

    public class ValidationErrorResponse
    {
        public Dictionary<string, string[]> Errors { get; set; }
    }
}