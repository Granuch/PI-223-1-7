using Newtonsoft.Json;
using System.Text;
using UI.Controllers;
using UI.Models.DTOs;
using UI.Models.ViewModels;

namespace UI.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;
        private readonly IConfiguration _configuration;

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            var baseUrl = _configuration["ApiSettings:BaseUrl"];

            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = "https://localhost:7164";
                _logger.LogWarning("ApiSettings:BaseUrl не знайдено в конфігурації. Використовується значення за замовчуванням: {BaseUrl}", baseUrl);
            }

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            _logger.LogInformation("ApiService ініціалізовано з BaseUrl: {BaseUrl}", baseUrl);
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

        private string ConvertIntToGenre(int genreId)
        {
            return genreId switch
            {
                1 => "Художня література",
                2 => "Наукова",
                3 => "Історична",
                4 => "Біографія",
                5 => "Фентезі",
                6 => "Детектив",
                7 => "Романтика",
                8 => "Трилер",
                _ => "Невідомо"
            };
        }

        private string ConvertIntToBookType(int typeId)
        {
            return typeId switch
            {
                0 => "Фізична",
                1 => "Цифрова",
                2 => "Аудіо",
                _ => "Невідомо"
            };
        }

        public async Task<ApiResponse<BookDTO>> GetBookByIdAsync(int id)
        {
            try
            {
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
        private int ConvertGenreToNumber(string genre)
        {
            return genre switch
            {
                "Fiction" => 0,
                "Science" => 1,
                "History" => 2,
                "Biography" => 3,
                "Fantasy" => 4,
                "Mystery" => 5,
                "Romance" => 6,
                "Thriller" => 7,
                "Drama" => 8,
                _ => 0 // За замовчуванням Fiction
            };
        }
        private int ConvertTypeToNumber(string type)
        {
            return type switch
            {
                "Physical" => 0,
                "Digital" => 1,
                "Audio" => 2,
                _ => 0 // За замовчуванням Physical
            };
        }

        public async Task<ApiResponse<object>> UpdateBookAsync(int id, BookDTO book)
        {
            try
            {
                _logger.LogInformation("Updating book {BookId} with data: {@Book}", id, book);

                // Логуємо початкові дані
                _logger.LogInformation("Original book data - Title: {Title}, Author: {Author}, Genre: {Genre}, Type: {Type}, Year: {Year}",
                    book.Title, book.Author, book.Genre, book.Type, book.Year);

                // Створюємо об'єкт у форматі API
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
                _logger.LogInformation("Sending JSON to API: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/books/Update/{id}", content);

                _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);

                // ДОДАЄМО ДЕТАЛЬНЕ ЛОГУВАННЯ ПОМИЛКИ
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API response content: {Content}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API returned error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Помилка API: {response.StatusCode} - {responseContent}"
                    };
                }

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Книга успішно оновлена"
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
        private int ConvertGenreToInt(string genre)
        {
            var result = genre switch
            {
                "Fiction" => 1,
                "Science" => 2,
                "History" => 3,
                "Biography" => 4,
                "Fantasy" => 5,
                "Mystery" => 6,
                "Romance" => 7,
                "Thriller" => 8,
                _ => 0
            };

            _logger.LogInformation("Converting genre '{Genre}' to {Result}", genre, result);
            return result;
        }
        private int ConvertBookTypeToInt(string type)
        {
            var result = type switch
            {
                "Physical" => 0,
                "Digital" => 1,
                "Audio" => 2,
                _ => 0
            };

            _logger.LogInformation("Converting type '{Type}' to {Result}", type, result);
            return result;
        }

        public async Task<ApiResponse<object>> DeleteBookAsync(int id)
        {
            try
            {
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
        private int ConvertGenreToInt(object genre)
        {
            if (genre is int intGenre) return intGenre;
            if (genre is string strGenre && int.TryParse(strGenre, out int parsedGenre)) return parsedGenre;
            if (genre is Enum enumGenre) return Convert.ToInt32(enumGenre);
            return 0; // default value
        }
        private int ConvertTypeToInt(object type)
        {
            if (type is int intType) return intType;
            if (type is string strType && int.TryParse(strType, out int parsedType)) return parsedType;
            if (type is Enum enumType) return Convert.ToInt32(enumType);
            return 0; // default value
        }

        public async Task<ApiResponse<object>> DeleteOrderAsync(int id)
        {
            try
            {
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

    }
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }

    public class UserResponse
    {
        public bool IsAuthenticated { get; set; }
        public UserInfo User { get; set; }
    }
}
