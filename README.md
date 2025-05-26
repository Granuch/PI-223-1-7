#  Library Management System

> [!NOTE]
> Система управління бібліотекою - це сучасний веб-додаток, побудований на мікросервісній архітектурі з використанням ASP.NET Core 9.0. Система забезпечує повне управління книгами, замовленнями та користувачами з розподіленою обробкою даних через API Gateway.

##  Ключові особливості

-  Мікросервісна архітектура з API Gateway (Ocelot)
-  Автентифікація Cookie-based аутентифікацією
-  Гнучка система ролей (Administrator, Manager, RegisteredUser)
-  Повне управління каталогом книг з фільтрацією та пошуком
-  Система замовлень з відстеженням статусу
-  Сучасний веб-інтерфейс на ASP.NET Core MVC
-  Комплексне тестування з NUnit та Moq
-  Безпека даних з Data Protection API

##  Архітектура системи

### Мікросервіси

| Сервіс | Порт | Призначення | Технології |
|--------|------|-------------|-------------|
| **API Gateway** | 5003 | Точка входу, маршрутизація запитів | Ocelot, ASP.NET Core |
| **UI Service** | 7280 | Веб-інтерфейс користувача | ASP.NET Core MVC, Bootstrap |
| **Account Service** | 5010 | Автентифікація та авторизація | ASP.NET Core Identity, JWT |
| **Books Service** | 5001 | Управління каталогом книг | ASP.NET Core Web API |
| **Orders Service** | 5000 | Система замовлень | ASP.NET Core Web API |
| **Admin Service** | 5005 | Адміністративне управління | ASP.NET Core Web API |

### Шари архітектури

```
 Library-Management-System/
├──  UI/                          # Presentation Layer (MVC)
├──  ApiGateway/                  # API Gateway (Ocelot)
├──  AccountController/           # Authentication Service
├──  BooksService/                # Books Microservice
├──  OrdersService/               # Orders Microservice
├──  AdminUserService/            # Admin Management Service
├──  DAL/ (PI-223-1-7)            # Data Access Layer
├──  BLL/                         # Business Logic Layer
├──  Mapping/                     # DTOs and AutoMapper
└──  Tests/                       # Unit Tests
```

##  Швидкий старт

### Передумови

> [!IMPORTANT]
> Переконайтеся, що у вас встановлено:
> - **.NET 9.0 SDK**
> - **SQL Server** (LocalDB або повна версія)
> - **Visual Studio 2022** або **VS Code**

###  Запуск системи

1. **Клонування репозиторію**
```bash
git clone https://github.com/your-username/library-management-system.git
cd library-management-system
```

2. **Налаштування бази даних**
```bash
cd PI-223-1-7
dotnet ef database update
```

3. **Запуск мікросервісів** (в окремих терміналах)

```bash
# API Gateway
cd ApiGateway
dotnet run --urls="https://localhost:5003"

# Account Service
cd AccountController
dotnet run --urls="http://localhost:5010"

# Books Service
cd BooksService
dotnet run --urls="http://localhost:5001"

# Orders Service
cd OrdersService
dotnet run --urls="http://localhost:5000"

# Admin Service
cd AdminUserService
dotnet run --urls="http://localhost:5005"

# UI Service
cd UI
dotnet run --urls="https://localhost:7280"
```

4. **Доступ до додатку**
   - **Веб-додаток**: https://localhost:7280
   - **API Gateway**: https://localhost:5003

### 👤 Тестові облікові записи

> [!TIP]
> Система автоматично створює тестових користувачів:

| Роль | Email | Пароль | Доступ |
|------|-------|---------|--------|
| **Administrator** | admin@example.com | Admin123 | Повний доступ |
| **Manager** | manager@example.com | Manager123 | Управління книгами та замовленнями |
| **User** | user1@example.com | User123! | Перегляд та замовлення книг |

##  Система безпеки

### Автентифікація та авторизація

- **Автентифікація**: Cookie-based сесії
- **Data Protection API** для захисту cookies між сервісами
- **Cross-service authentication** через спільні ключі шифрування

### Ролі та дозволи

| Роль | Дозволи |
|------|---------|
|  Guest | Перегляд каталогу, пошук книг |
|  RegisteredUser | + Замовлення книг, перегляд власних замовлень |
|  Manager | + Управління каталогом, перегляд всіх замовлень |
|  Administrator | + Управління користувачами, видалення книг |

##  API Documentation

###  API Gateway Routes

#### Books API
```http
GET    /api/books/getall          # Отримати всі книги
GET    /api/books/getbyid/{id}    # Книга за ID
GET    /api/books/filter          # Фільтрація книг
POST   /api/books/createbook      # Створити книгу [Manager+]
PUT    /api/books/updatebook/{id} # Оновити книгу [Manager+]
DELETE /api/books/delete/{id}     # Видалити книгу [Admin]
```

#### Orders API
```http
GET    /api/orders/getall         # Всі замовлення [Manager+]
POST   /api/orders/createnew      # Створити замовлення [User+]
GET    /api/orders/findspecific/{id} # Замовлення за ID
DELETE /api/orders/delete/{id}    # Видалити замовлення [User+]
```

#### Authentication API
```http
POST   /api/account/reg           # Реєстрація
POST   /api/account/log           # Вхід
POST   /api/account/logout        # Вихід
GET    /api/account/stat          # Статус автентифікації
```

#### Admin API
```http
GET    /api/users/getall          # Всі користувачі [Admin]
POST   /api/users/createuser      # Створити користувача [Admin]
PUT    /api/users/updateuser      # Оновити користувача [Admin]
DELETE /api/users/deleteuser      # Видалити користувача [Admin]
POST   /api/users/assignrole      # Призначити роль [Admin]
```

##  Модель даних

### Основні сутності

```csharp
// Book Entity
public class Book
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public GenreTypes Genre { get; set; }     // Fiction, Science, History, etc.
    public BookTypes Type { get; set; }       // Physical, Digital, Audio
    public bool IsAvailable { get; set; }
    public DateTime Year { get; set; }
    public virtual ICollection<Order> Orders { get; set; }
}

// Order Entity
public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int BookId { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatusTypes Type { get; set; } // Pending, Approved, Completed, Cancelled
    public virtual Book Book { get; set; }
}

// Application User
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public virtual ICollection<Order> Orders { get; set; }
}
```

##  Тестування

### Запуск тестів
```bash
cd Tests
dotnet test --logger:"console;verbosity=detailed"
```

### Покриття тестами
- **BookService**: CRUD операції, фільтрація, сортування, доступність
- **OrderService**: Управління замовленнями, валідація
- **UserService**: Управління користувачами, ролями, автентифікація

### Тестові фреймворки
- **NUnit** - основний фреймворк тестування
- **Moq** - створення mock об'єктів
- **AutoMapper** - тестування маппінгу

##  Технології та залежності

### Backend
- **ASP.NET Core 9.0** - основний фреймворк
- **Entity Framework Core** - ORM для роботи з базою даних
- **ASP.NET Core Identity** - автентифікація та авторизація
- **Ocelot** - API Gateway
- **AutoMapper** - маппінг між моделями

### Frontend
- **ASP.NET Core MVC** - веб-фреймворк
- **Bootstrap 5** - CSS фреймворк
- **jQuery** - JavaScript бібліотека
- **Razor Pages** - шаблонізатор

### База даних
- **SQL Server** - основна база даних
- **Entity Framework Migrations** - управління схемою БД

### Тестування
- **NUnit** - unit testing framework
- **Moq** - mocking library
- **coverlet** - code coverage

## Конфігурація

### appsettings.json (приклад)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LibraryDb;Trusted_Connection=True;"
  },
  "ApiSettings": {
    "BaseUrl": "https://localhost:5003"
  }
}
```

### Ocelot Configuration
```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/Books/GetAll",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{"Host": "localhost", "Port": 5001}],
      "UpstreamPathTemplate": "/api/books/getall",
      "UpstreamHttpMethod": ["GET"]
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "https://localhost:5003"
  }
}
```

##  Можливості для розвитку

### Поточні функції
-  Повна мікросервісна архітектура
-  Автентифікація через API Gateway
-  Система ролей та дозволів
-  Управління каталогом книг
-  Система замовлень
-  Адміністративна панель
-  Unit тестування

### Майбутні вдосконалення
-  **Containerization** з Docker
-  **Health Checks** для мікросервісів
-  **Логування** з Serilog/ELK Stack
-  **Internationalization** (i18n)
-  **Performance monitoring** з Application Insights
-  **JWT tokens** перехід на використання JWT токенів

##  Внесок у проект

### Як зробити внесок

1. **Fork** репозиторій
2. Створіть гілку для нової функції:
   ```bash
   git checkout -b feature/amazing-feature
   ```
3. Зробіть коміт змін:
   ```bash
   git commit -m 'Add amazing feature'
   ```
4. Відправте в гілку:
   ```bash
   git push origin feature/amazing-feature
   ```
5. Створіть **Pull Request**

### Coding Standards
- Використовуйте **C# naming conventions**
- Додавайте **XML documentation** для публічних методів
- Пишіть **unit tests** для нової функціональності
- Дотримуйтесь **SOLID principles**

## Troubleshooting

### Часті проблеми

**Проблема**: Сервіси не можуть з'єднатися
```bash
# Перевірте, чи запущені всі сервіси на правильних портах
netstat -an | findstr "5003 5010 5001 5000 5005"
```

**Проблема**: Помилки автентифікації між сервісами
```bash
# Перевірте Data Protection ключі
ls C:\temp\keys\
```

**Проблема**: База даних не ініціалізована
```bash
cd PI-223-1-7
dotnet ef database drop
dotnet ef database update
```

## Підтримка

Якщо у вас виникли питання або проблеми:

1. Перевірте [Issues](https://github.com/your-username/library-management-system/issues)
2. Створіть новий Issue з детальним описом
3. Надайте логи та кроки для відтворення проблеми

## Ліцензія

Цей проект ліцензовано під [MIT License](LICENSE).
