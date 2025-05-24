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

            var baseUrl = _configuration["ApiSettings:BaseUrl"];

            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = "https://localhost:7164";
                _logger.LogWarning("ApiSettings:BaseUrl не знайдено в конфігурації. Використовується значення за замовчуванням: {BaseUrl}", baseUrl);
            }

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            try
            {
                var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    _logger.LogInformation("Auth token restored from session");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring token from session");
            }

            _logger.LogInformation("ApiService ініціалізовано з BaseUrl: {BaseUrl}", baseUrl);
        }
        private void EnsureAuthToken()
        {
            try
            {
                var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");

                if (!string.IsNullOrEmpty(token))
                {
                    var currentToken = _httpClient.DefaultRequestHeaders.Authorization?.Parameter;

                    // Встановлюємо токен тільки якщо він відрізняється від поточного
                    if (currentToken != token)
                    {
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                        _logger.LogDebug("Auth token updated in HTTP client");
                    }
                }
                else
                {
                    // Видаляємо токен якщо його немає в сесії
                    if (_httpClient.DefaultRequestHeaders.Authorization != null)
                    {
                        _httpClient.DefaultRequestHeaders.Authorization = null;
                        _logger.LogDebug("Auth token removed from HTTP client");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring auth token");
            }
        }

        public async Task<ApiResponse<UserResponse>> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/account/register", content);
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
                _logger.LogInformation("Sending JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/account/login", content);

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    // Парсимо як dynamic спочатку для діагностики
                    var dynamicResult = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    _logger.LogInformation("Dynamic user data: {UserData}", (object)JsonConvert.SerializeObject(dynamicResult.user));

                    // Створюємо UserInfo об'єкт вручну з dynamic об'єкта
                    var userInfo = new UserInfo
                    {
                        UserId = dynamicResult.user?.userId?.ToString(),
                        Id = dynamicResult.user?.id?.ToString(),
                        Email = dynamicResult.user?.email?.ToString(),
                        FirstName = dynamicResult.user?.firstName?.ToString(),
                        LastName = dynamicResult.user?.lastName?.ToString(),
                        Roles = dynamicResult.user?.roles?.ToObject<List<string>>() ?? new List<string>()
                    };

                    // Використовуємо безпечне логування без dynamic
                    _logger.LogInformation("Manually created UserInfo: UserId='{UserId}', Id='{Id}', Email='{Email}'",
                        userInfo.UserId ?? "null", userInfo.Id ?? "null", userInfo.Email ?? "null");

                    // Додаткова перевірка і логування
                    if (string.IsNullOrEmpty(userInfo.UserId))
                    {
                        var rawUserId = dynamicResult.user?.userId?.ToString() ?? "null";
                        _logger.LogError("Failed to extract UserId from dynamic object. Raw userId value: {RawUserId}", (object)rawUserId);
                    }

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
                var response = await _httpClient.PostAsync("api/account/logout", null);

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
                var response = await _httpClient.GetAsync("api/account/status");
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

        public async Task<ApiResponse<IEnumerable<BookDTO>>> GetAllBooksAsync()
        {
            try
            {
                EnsureAuthToken();

                var response = await _httpClient.GetAsync("api/books/GetAll");
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

                return new ApiResponse<IEnumerable<BookDTO>>
                {
                    Success = false,
                    Message = "Помилка отримання книг"
                };
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
                EnsureAuthToken();

                _logger.LogInformation("Filtering books with params: sortOrder={SortOrder}, searchString={SearchString}, genre={Genre}, type={Type}",
                    sortOrder, searchString, genre, type);

                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(sortOrder))
                    queryParams.Add($"sortOrder={Uri.EscapeDataString(sortOrder)}");

                if (!string.IsNullOrEmpty(searchString))
                    queryParams.Add($"searchString={Uri.EscapeDataString(searchString)}");

                // Конвертуємо genre з рядка в число
                if (!string.IsNullOrEmpty(genre))
                {
                    int genreId = ConvertGenreToInt(genre);
                    queryParams.Add($"genre={genreId}");
                    _logger.LogInformation("Converted genre '{Genre}' to {GenreId}", genre, genreId);
                }

                // Конвертуємо type з рядка в число
                if (!string.IsNullOrEmpty(type))
                {
                    int typeId = ConvertBookTypeToInt(type);
                    queryParams.Add($"type={typeId}");
                    _logger.LogInformation("Converted type '{Type}' to {TypeId}", type, typeId);
                }

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var fullUrl = $"api/books/Filter{queryString}";

                _logger.LogInformation("Making request to: {Url}", fullUrl);

                var response = await _httpClient.GetAsync(fullUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API response: {StatusCode}, Content length: {ContentLength}",
                    response.StatusCode, responseContent.Length);

                if (response.IsSuccessStatusCode)
                {
                    // Десеріалізуємо без зайвої конвертації - BookDTO сам обробить відображення
                    var books = JsonConvert.DeserializeObject<IEnumerable<BookDTO>>(responseContent);

                    return new ApiResponse<IEnumerable<BookDTO>>
                    {
                        Success = true,
                        Data = books
                    };
                }

                _logger.LogError("API returned error: {StatusCode} - {Content}", response.StatusCode, responseContent);

                return new ApiResponse<IEnumerable<BookDTO>>
                {
                    Success = false,
                    Message = $"Помилка отримання книг: {response.StatusCode}"
                };
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
                EnsureAuthToken();

                var response = await _httpClient.GetAsync($"api/books/GetById/{id}");
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

                return new ApiResponse<BookDTO>
                {
                    Success = false,
                    Message = "Книга не знайдена"
                };
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
                EnsureAuthToken();

                _logger.LogInformation("Creating book: {Title} by {Author}", book.Title, book.Author);

                // Конвертуємо Genre та Type в числа (enum values)
                int genreValue = ConvertGenreToNumber(book.Genre);
                int typeValue = ConvertTypeToNumber(book.Type);

                // API очікує точно такі поля, як у Swagger (БЕЗ OrderId)
                var apiBookData = new
                {
                    id = 0,                          // Для нової книги завжди 0
                    name = book.Title,               // Маленька літера!
                    author = book.Author,            // Маленька літера!
                    description = book.Description ?? "", // Маленька літера!
                    genre = genreValue,              // Число
                    type = typeValue,                // Число
                    isAvailable = book.IsAvailable,  // Правильна назва
                    year = book.Year                 // DateTime
                                                     // OrderId НЕ включаємо - він не потрібен при створенні
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    Formatting = Formatting.Indented
                };

                var json = JsonConvert.SerializeObject(apiBookData, settings);
                _logger.LogInformation("Sending JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/books/Create", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Create Book Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Create Book Response: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    // Створюємо BookDTO вручну з відповіді API
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
                        OrderId = null // Для нової книги OrderId порожній
                    };

                    return new ApiResponse<BookDTO>
                    {
                        Success = true,
                        Data = createdBook
                    };
                }

                // Обробляємо помилки валідації
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    var errorMessage = "Помилка створення книги";

                    if (errorResponse?.errors != null)
                    {
                        var errors = new List<string>();
                        foreach (var error in errorResponse.errors)
                        {
                            var fieldName = error.Name;
                            var fieldErrors = error.Value;
                            if (fieldErrors != null)
                            {
                                foreach (var fieldError in fieldErrors)
                                {
                                    errors.Add($"{fieldName}: {fieldError}");
                                }
                            }
                        }
                        errorMessage = string.Join("; ", errors);
                    }

                    return new ApiResponse<BookDTO>
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
                catch
                {
                    return new ApiResponse<BookDTO>
                    {
                        Success = false,
                        Message = $"Помилка створення книги. Статус: {response.StatusCode}. Відповідь: {responseContent}"
                    };
                }
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
                EnsureAuthToken();

                _logger.LogInformation("Starting UpdateBookAsync for book {BookId}", id);
                _logger.LogInformation("Input book data - Title: {Title}, Author: {Author}, Genre: {Genre}, Type: {Type}, Year: {Year}, IsAvailable: {IsAvailable}",
                    book.Title, book.Author, book.Genre, book.Type, book.Year, book.IsAvailable);

                // Конвертуємо Genre та Type
                int genreValue = ConvertGenreToInt(book.Genre);
                int typeValue = ConvertBookTypeToInt(book.Type);

                _logger.LogInformation("Converted values - Genre: '{Genre}' -> {GenreValue}, Type: '{Type}' -> {TypeValue}",
                    book.Genre, genreValue, book.Type, typeValue);

                // Створюємо об'єкт у форматі API
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
                _logger.LogInformation("Sending JSON to API: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Спробуйте різні URL
                string[] possibleUrls = {
            $"api/books/Update/{id}",      // Поточний
            $"api/books/{id}",             // REST стандарт
            $"Books/Update/{id}",          // Без api prefix
            $"Books/{id}"                  // REST без prefix
        };

                foreach (var url in possibleUrls)
                {
                    try
                    {
                        _logger.LogInformation("Trying URL: {Url}", url);
                        var response = await _httpClient.PutAsync(url, content);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        _logger.LogInformation("Response for {Url}: Status={StatusCode}, Content={Content}",
                            url, response.StatusCode, responseContent);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Successfully updated book with URL: {Url}", url);
                            return new ApiResponse<object>
                            {
                                Success = true,
                                Message = "Книга успішно оновлена"
                            };
                        }

                        // Якщо це не 404, то endpoint існує, але є проблема
                        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogError("API error with URL {Url}: {StatusCode} - {Content}",
                                url, response.StatusCode, responseContent);

                            return new ApiResponse<object>
                            {
                                Success = false,
                                Message = $"Помилка API ({response.StatusCode}): {responseContent}"
                            };
                        }
                    }
                    catch (Exception urlEx)
                    {
                        _logger.LogWarning(urlEx, "Exception with URL {Url}", url);
                    }
                }

                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Не вдалося знайти робочий API endpoint для оновлення книги"
                };
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
                EnsureAuthToken();

                var response = await _httpClient.DeleteAsync($"api/books/Delete/{id}");

                return new ApiResponse<object>
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Книга видалена" : "Помилка видалення книги"
                };
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
                EnsureAuthToken();

                var response = await _httpClient.GetAsync("api/books/GetUserOrders");
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

                return new ApiResponse<IEnumerable<BookDTO>>
                {
                    Success = false,
                    Message = "Помилка отримання замовлень"
                };
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
                EnsureAuthToken();

                var response = await _httpClient.GetAsync($"api/books/CheckAvailability/{id}");
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

                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Помилка перевірки доступності"
                };
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

        public async Task<ApiResponse<IEnumerable<OrderDTO>>> GetAllOrdersAsync()
        {
            try
            {
                EnsureAuthToken();

                // ВИПРАВЛЕНО: Orders з великої літери та правильний endpoint
                var response = await _httpClient.GetAsync("Orders/Getall");
                var responseContent = await response.Content.ReadAsStringAsync();

                // Додаємо логування для діагностики
                _logger.LogInformation("Orders API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Orders API Response: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var orders = JsonConvert.DeserializeObject<IEnumerable<OrderDTO>>(responseContent);
                    return new ApiResponse<IEnumerable<OrderDTO>>
                    {
                        Success = true,
                        Data = orders
                    };
                }

                return new ApiResponse<IEnumerable<OrderDTO>>
                {
                    Success = false,
                    Message = $"Помилка отримання замовлень. Статус: {response.StatusCode}"
                };
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

        public async Task<ApiResponse<string>> GetUserEmailByIdAsync(string userId)
        {
            try
            {
                EnsureAuthToken();

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

                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Користувач не знайдений"
                };
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

        public async Task<ApiResponse<OrderDTO>> GetOrderByIdAsync(int id)
        {
            try
            {
                EnsureAuthToken();

                // ВИПРАВЛЕНО: Orders з великої літери
                var response = await _httpClient.GetAsync($"Orders/GetSpecific/{id}");
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

                return new ApiResponse<OrderDTO>
                {
                    Success = false,
                    Message = "Замовлення не знайдено"
                };
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
                EnsureAuthToken();

                _logger.LogInformation("Creating order for user {UserId} and book {BookId}", order.UserId, order.BookId);

                // Отримуємо дані книги для створення повного об'єкта замовлення
                var bookResult = await GetBookByIdAsync(order.BookId);
                if (!bookResult.Success || bookResult.Data == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Не вдалося отримати інформацію про книгу"
                    };
                }

                // Перевіряємо, чи книга доступна
                if (!bookResult.Data.IsAvailable)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Книга вже недоступна для замовлення"
                    };
                }

                // Створюємо об'єкт у форматі API (БЕЗ зміни статусу книги тут)
                var orderData = new
                {
                    id = 0, // Для нового замовлення
                    userId = order.UserId,
                    bookId = order.BookId,
                    orderDate = order.OrderDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    type = 0, // Pending = 0 (використовуємо enum значення)
                    book = new
                    {
                        id = bookResult.Data.Id,
                        name = bookResult.Data.Title,
                        author = bookResult.Data.Author,
                        description = bookResult.Data.Description ?? "",
                        genre = bookResult.Data.GenreId,
                        type = bookResult.Data.TypeId,
                        isAvailable = bookResult.Data.IsAvailable, // Зберігаємо поточний статус
                        year = bookResult.Data.Year.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                };

                var json = JsonConvert.SerializeObject(orderData);
                _logger.LogInformation("Sending order JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("Orders/CreateNewOrder", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Create order response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Замовлення успішно створено"
                    };
                }
                else
                {
                    _logger.LogError("API returned error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Помилка створення замовлення: {response.StatusCode}"
                    };
                }
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

        public async Task<ApiResponse<object>> UpdateBookAvailabilityAsync(int bookId, bool isAvailable)
        {
            try
            {
                EnsureAuthToken();

                _logger.LogInformation("Updating book {BookId} availability to {IsAvailable}", bookId, isAvailable);

                // Отримуємо поточні дані книги
                var bookResult = await GetBookByIdAsync(bookId);
                if (!bookResult.Success || bookResult.Data == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Не вдалося отримати дані книги"
                    };
                }

                // Оновлюємо тільки статус доступності
                var updateData = new
                {
                    id = bookResult.Data.Id,
                    name = bookResult.Data.Title,
                    author = bookResult.Data.Author,
                    description = bookResult.Data.Description ?? "",
                    genre = bookResult.Data.GenreId,
                    type = bookResult.Data.TypeId,
                    isAvailable = isAvailable, // Оновлюємо статус
                    year = bookResult.Data.Year.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/books/Update/{bookId}", content);

                return new ApiResponse<object>
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Статус книги оновлено" : "Помилка оновлення статусу книги"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book availability for book {BookId}", bookId);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка оновлення статусу книги"
                };
            }
        }

        public async Task<ApiResponse<object>> CancelOrderAsync(int orderId, string userId)
        {
            try
            {
                EnsureAuthToken();

                _logger.LogInformation("Cancelling order {OrderId} for user {UserId}", orderId, userId);

                // Спочатку отримуємо дані замовлення
                var orderResult = await GetOrderByIdAsync(orderId);
                if (!orderResult.Success || orderResult.Data == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Замовлення не знайдено"
                    };
                }

                // Перевіряємо, чи користувач має право скасувати це замовлення
                if (orderResult.Data.UserId != userId)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Ви не маєте права скасувати це замовлення"
                    };
                }

                // Перевіряємо, чи замовлення активне
                if (orderResult.Data.Type != 1) // 1 = Активне
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Можна скасувати тільки активні замовлення"
                    };
                }

                // Видаляємо замовлення
                var response = await _httpClient.DeleteAsync($"Orders/Delete/{orderId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Cancel order response: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    // Після успішного скасування замовлення робимо книгу доступною
                    var updateBookResult = await UpdateBookAvailabilityAsync(orderResult.Data.BookId, true);
                    if (!updateBookResult.Success)
                    {
                        _logger.LogWarning("Order cancelled but failed to update book availability: {Message}", updateBookResult.Message);
                    }

                    return new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Замовлення успішно скасовано"
                    };
                }
                else
                {
                    _logger.LogError("API returned error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Помилка скасування замовлення: {response.StatusCode}"
                    };
                }
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

        public async Task<ApiResponse<object>> UpdateOrderAsync(int id, OrderDTO order)
        {
            try
            {
                EnsureAuthToken();

                _logger.LogInformation("Updating order {OrderId} with data: {@Order}", id, order);

                // Спочатку отримуємо поточні дані замовлення
                var currentOrderResult = await GetOrderByIdAsync(id);
                if (!currentOrderResult.Success || currentOrderResult.Data?.Book == null)
                {
                    _logger.LogError("Cannot get current order data for update");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Помилка отримання поточних даних замовлення"
                    };
                }

                var currentBook = currentOrderResult.Data.Book;

                // Якщо BookId змінився, потрібно отримати нову книгу
                if (order.BookId != currentBook.Id)
                {
                    var newBookResult = await GetBookByIdAsync(order.BookId);
                    if (newBookResult.Success && newBookResult.Data != null)
                    {
                        currentBook = newBookResult.Data;
                    }
                    else
                    {
                        _logger.LogError("Cannot get new book data for BookId: {BookId}", order.BookId);
                        return new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Помилка отримання даних книги"
                        };
                    }
                }

                // Створюємо об'єкт точно у форматі API
                var updateData = new
                {
                    id = order.Id,
                    userId = order.UserId,
                    bookId = order.BookId,
                    orderDate = order.OrderDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), // ISO format
                    type = order.Type,
                    book = new
                    {
                        id = currentBook.Id,
                        name = currentBook.Title ?? currentBook.Title ?? "",  // Підтримка обох варіантів
                        author = currentBook.Author ?? "",
                        description = currentBook.Description ?? "",
                        genre = ConvertGenreToInt(currentBook.Genre),
                        type = ConvertTypeToInt(currentBook.Type),
                        isAvailable = currentBook.IsAvailable,
                        year = currentBook.Year.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") // ISO format
                    }
                };

                var json = JsonConvert.SerializeObject(updateData);
                _logger.LogInformation("Sending JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"Orders/Update?orderId={id}", content);

                _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                }

                return new ApiResponse<object>
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Замовлення оновлено" : $"Помилка оновлення замовлення: {response.StatusCode}"
                };
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
                EnsureAuthToken();

                // ВИПРАВЛЕНО: Orders з великої літери
                var response = await _httpClient.DeleteAsync($"Orders/Delete?id={id}");

                return new ApiResponse<object>
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Замовлення видалено" : "Помилка видалення замовлення"
                };
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


        public async Task<ApiResponse<IEnumerable<UserDTO>>> GetAllUsersAsync()
        {
            try
            {
                EnsureAuthToken();

                _logger.LogInformation("Getting all users");

                var response = await _httpClient.GetAsync("api/AdminUsers/GetAllUsers");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("API Response Content length: {ContentLength}", responseContent.Length);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<UserDTO>>>(responseContent);
                    return apiResponse;
                }

                _logger.LogError("API returned error: {StatusCode} - {Content}", response.StatusCode, responseContent);

                return new ApiResponse<IEnumerable<UserDTO>>
                {
                    Success = false,
                    Message = $"Помилка отримання користувачів: {response.StatusCode}"
                };
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
                EnsureAuthToken();


                _logger.LogInformation("Getting user by ID: {UserId}", id);

                var response = await _httpClient.GetAsync($"api/AdminUsers/GetUserById?id={Uri.EscapeDataString(id)}");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<UserDTO>>(responseContent);
                    return apiResponse;
                }

                _logger.LogError("API returned error: {StatusCode} - {Content}", response.StatusCode, responseContent);

                return new ApiResponse<UserDTO>
                {
                    Success = false,
                    Message = $"Помилка отримання користувача: {response.StatusCode}"
                };
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
                _logger.LogInformation("Creating user: {Email}", request.Email);

                // Переконуємось що роль встановлена
                if (string.IsNullOrEmpty(request.Role))
                {
                    request.Role = "RegisteredUser";
                }

                // Перевіряємо токен перед запитом
                EnsureAuthToken();

                var json = JsonConvert.SerializeObject(request);
                _logger.LogInformation("Sending JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/AdminUsers/CreateUser", content);

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                        return apiResponse ?? new ApiResponse<object> { Success = true };
                    }
                    catch (JsonSerializationException)
                    {
                        // Якщо не можемо десеріалізувати як ApiResponse, повертаємо успіх
                        return new ApiResponse<object> { Success = true, Message = "Користувач створений успішно" };
                    }
                }

                // ВИПРАВЛЕНО: краща обробка помилок валідації
                try
                {
                    // Спробуємо розпарсити як стандартну помилку ASP.NET Core
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    var apiResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Помилка створення користувача"
                    };

                    // Перевіряємо чи є поле errors (помилки валідації)
                    if (errorResponse.errors != null)
                    {
                        var validationErrors = new Dictionary<string, IEnumerable<string>>();
                        var errorsList = new List<string>();

                        foreach (var error in errorResponse.errors)
                        {
                            string fieldName = error.Name;
                            var messages = new List<string>();

                            foreach (var message in error.Value)
                            {
                                string errorMessage = message.ToString();
                                messages.Add(errorMessage);
                                errorsList.Add($"{fieldName}: {errorMessage}");
                            }

                            validationErrors[fieldName] = messages;
                        }

                        apiResponse.ValidationErrors = validationErrors;
                        apiResponse.Errors = errorsList;
                        apiResponse.Message = "Помилки валідації: " + string.Join("; ", errorsList);
                    }
                    else if (errorResponse.message != null)
                    {
                        apiResponse.Message = errorResponse.message.ToString();
                    }

                    return apiResponse;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing error response");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Помилка створення користувача: {response.StatusCode}. {responseContent}"
                    };
                }
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
                _logger.LogInformation("Creating admin: {Email}", request.Email);

                // ВИПРАВЛЕНО: встановлюємо правильну роль
                request.Role = "Administrator";

                EnsureAuthToken();

                var json = JsonConvert.SerializeObject(request);
                _logger.LogInformation("Sending JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/AdminUsers/CreateAdmin", content);

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
                _logger.LogInformation("Creating manager: {Email}", request.Email);

                // ВИПРАВЛЕНО: встановлюємо правильну роль
                request.Role = "Manager";

                EnsureAuthToken();

                var json = JsonConvert.SerializeObject(request);
                _logger.LogInformation("Sending JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/AdminUsers/CreateManager", content);

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

        private async Task<ApiResponse<object>> ProcessCreateUserResponse(HttpResponseMessage response, string userType)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("API Response Content: {Content}", responseContent);

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

            // Обробка помилок (аналогічно до CreateUserAsync)
            try
            {
                var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                var apiResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Помилка створення {userType}"
                };

                if (errorResponse.errors != null)
                {
                    var errorsList = new List<string>();
                    foreach (var error in errorResponse.errors)
                    {
                        foreach (var message in error.Value)
                        {
                            errorsList.Add($"{error.Name}: {message}");
                        }
                    }
                    apiResponse.Errors = errorsList;
                    apiResponse.Message = $"Помилки валідації при створенні {userType}: " + string.Join("; ", errorsList);
                }
                else if (errorResponse.message != null)
                {
                    apiResponse.Message = errorResponse.message.ToString();
                }

                return apiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing error response for {UserType}", userType);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Помилка створення {userType}: {response.StatusCode}"
                };
            }
        }

        public async Task<ApiResponse<object>> UpdateUserAsync(string id, UpdateUserRequest request)
        {
            try
            {
                EnsureAuthToken();

                _logger.LogInformation("Updating user: {UserId}", id);

                var json = JsonConvert.SerializeObject(request);
                _logger.LogInformation("Sending JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/AdminUsers/UpdateUser?id={Uri.EscapeDataString(id)}", content);

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return apiResponse;
                }

                var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                return errorResponse ?? new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Помилка оновлення користувача: {response.StatusCode}"
                };
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
                EnsureAuthToken();

                _logger.LogInformation("Deleting user: {UserId}", id);

                var response = await _httpClient.DeleteAsync($"api/AdminUsers/DeleteUser?id={Uri.EscapeDataString(id)}");

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return apiResponse;
                }

                var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                return errorResponse ?? new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Помилка видалення користувача: {response.StatusCode}"
                };
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
                EnsureAuthToken();

                _logger.LogInformation("Changing password for user: {UserId}", id);

                var json = JsonConvert.SerializeObject(request);
                // НЕ логуємо пароль з міркувань безпеки
                _logger.LogInformation("Sending password change request for user: {UserId}", id);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/AdminUsers/ChangePassword?id={Uri.EscapeDataString(id)}", content);

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return apiResponse;
                }

                var errorResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                return errorResponse ?? new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Помилка зміни паролю: {response.StatusCode}"
                };
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
                EnsureAuthToken();

                // Встановлюємо UserId в request
                request.UserId = id;

                _logger.LogInformation("Assigning role {RoleName} to user: {UserId}", request.RoleName, id);

                var json = JsonConvert.SerializeObject(request);
                _logger.LogInformation("Sending JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Передаємо id як параметр URL
                var response = await _httpClient.PostAsync($"api/AdminUsers/AssignRole?id={Uri.EscapeDataString(id)}", content);

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                        return apiResponse ?? new ApiResponse<object> { Success = true, Message = "Роль призначена успішно" };
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Failed to deserialize successful response");
                        return new ApiResponse<object> { Success = true, Message = "Роль призначена успішно" };
                    }
                }

                return await HandleErrorResponse(responseContent, response.StatusCode, "Помилка призначення ролі");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleName} to user: {UserId}. Exception type: {ExceptionType}, Message: {ExceptionMessage}",
                    request?.RoleName, id, ex.GetType().Name, ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }

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
                EnsureAuthToken();

                // Встановлюємо UserId в request
                request.UserId = id;

                _logger.LogInformation("Removing role {RoleName} from user: {UserId}", request.RoleName, id);

                var json = JsonConvert.SerializeObject(request);
                _logger.LogInformation("Sending JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Передаємо id як параметр URL
                var response = await _httpClient.PostAsync($"api/AdminUsers/RemoveRole?id={Uri.EscapeDataString(id)}", content);

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                        return apiResponse ?? new ApiResponse<object> { Success = true, Message = "Роль видалена успішно" };
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Failed to deserialize successful response");
                        return new ApiResponse<object> { Success = true, Message = "Роль видалена успішно" };
                    }
                }

                return await HandleErrorResponse(responseContent, response.StatusCode, "Помилка видалення ролі");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleName} from user: {UserId}. Exception type: {ExceptionType}, Message: {ExceptionMessage}",
                    request?.RoleName, id, ex.GetType().Name, ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }

                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Помилка з'єднання з сервером: {ex.Message}"
                };
            }
        }

        private async Task<ApiResponse<object>> HandleErrorResponse(string responseContent, HttpStatusCode statusCode, string defaultErrorMessage)
        {
            _logger.LogInformation("HandleErrorResponse called with status: {StatusCode}, content: {Content}", statusCode, responseContent);

            try
            {
                // Спробуємо десеріалізувати як стандартну відповідь API
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                if (apiResponse != null)
                {
                    _logger.LogInformation("Successfully deserialized as ApiResponse");
                    return apiResponse;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize as ApiResponse");
                // Якщо не вдалося як ApiResponse, спробуємо як ValidationProblemDetails
                try
                {
                    var validationError = JsonConvert.DeserializeObject<ValidationErrorResponse>(responseContent);
                    if (validationError?.Errors != null)
                    {
                        _logger.LogInformation("Successfully deserialized as ValidationErrorResponse");
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
                catch (JsonException ex2)
                {
                    _logger.LogError(ex2, "Failed to deserialize as ValidationErrorResponse");
                    // Якщо не ValidationProblemDetails, просто поверніть загальну помилку
                }
            }

            _logger.LogInformation("Returning default error message");
            return new ApiResponse<object>
            {
                Success = false,
                Message = $"{defaultErrorMessage}: {statusCode}"
            };
        }

        public async Task<ApiResponse<IEnumerable<string>>> GetUserRolesAsync(string id)
        {
            try
            {
                EnsureAuthToken();

                _logger.LogInformation("Getting roles for user: {UserId}", id);

                var response = await _httpClient.GetAsync($"api/AdminUsers/GetUserRoles?id={Uri.EscapeDataString(id)}");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("API Response Content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Спочатку спробуємо десеріалізувати як успішну відповідь
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<string>>>(responseContent);
                        if (apiResponse != null && apiResponse.Success)
                        {
                            return apiResponse;
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Failed to deserialize as ApiResponse<IEnumerable<string>>. Response: {Response}", responseContent);
                    }
                }

                // Якщо десеріалізація не вдалася або статус не успішний, обробляємо як помилку
                try
                {
                    // Спробуємо десеріалізувати як помилку
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    string errorMessage = "Помилка отримання ролей користувача";

                    // Спробуємо витягти повідомлення про помилку
                    if (errorResponse?.message != null)
                    {
                        errorMessage = errorResponse.message.ToString();
                    }
                    else if (errorResponse?.errors != null)
                    {
                        // Якщо є помилки валідації
                        var errors = errorResponse.errors;
                        if (errors.id != null)
                        {
                            errorMessage = $"Помилка з ID користувача: {errors.id}";
                        }
                        else
                        {
                            errorMessage = "Помилки валідації: " + errorResponse.errors.ToString();
                        }
                    }

                    return new ApiResponse<IEnumerable<string>>
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize error response. Raw response: {Response}", responseContent);

                    return new ApiResponse<IEnumerable<string>>
                    {
                        Success = false,
                        Message = $"Неочікуваний формат відповіді від сервера. Статус: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles: {UserId}", id);
                return new ApiResponse<IEnumerable<string>>
                {
                    Success = false,
                    Message = "Помилка з'єднання з сервером"
                };
            }
        }

        public async Task<ApiResponse<IEnumerable<RoleDTO>>> GetAllRolesAsync()
        {
            try
            {
                EnsureAuthToken();

                _logger.LogInformation("Getting all roles");

                var response = await _httpClient.GetAsync("api/AdminUsers/GetAllRoles");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("API Response Content length: {ContentLength}", responseContent.Length);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<RoleDTO>>>(responseContent);
                    return apiResponse;
                }

                _logger.LogError("API returned error: {StatusCode} - {Content}", response.StatusCode, responseContent);

                return new ApiResponse<IEnumerable<RoleDTO>>
                {
                    Success = false,
                    Message = $"Помилка отримання ролей: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                return new ApiResponse<IEnumerable<RoleDTO>>
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

                // Отримуємо email з сесії
                var userDataJson = _httpContextAccessor.HttpContext?.Session.GetString("UserData");
                if (string.IsNullOrEmpty(userDataJson))
                {
                    _logger.LogWarning("User data not found in session");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Дані користувача не знайдені в сесії"
                    };
                }

                var userData = JsonConvert.DeserializeObject<UserInfo>(userDataJson);
                if (userData == null || string.IsNullOrEmpty(userData.Email))
                {
                    _logger.LogWarning("Invalid user data in session");
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Невірні дані користувача в сесії"
                    };
                }

                var request = new RefreshSessionRequest { Email = userData.Email };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling CheckAndRefreshSession for email: {Email}", userData.Email);

                var response = await _httpClient.PostAsync("api/Account/CheckAndRefreshSession", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("CheckAndRefreshSession API response: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Session refreshed successfully for: {Email}", userData.Email);
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                    return apiResponse ?? new ApiResponse<object> { Success = true };
                }

                _logger.LogWarning("Session refresh failed: {StatusCode} for email: {Email}", response.StatusCode, userData.Email);
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Помилка оновлення сесії: {response.StatusCode}"
                };
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
}
