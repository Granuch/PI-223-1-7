Library Management System
Система управління бібліотекою - це повнофункціональний веб-додаток для управління книгами, замовленнями та користувачами в бібліотеці. Проект реалізований з використанням ASP.NET Core Web API (бекенд) та ASP.NET Core MVC (фронтенд) з чистою архітектурою.
🏗️ Архітектура проекту
Проект побудований на принципах Clean Architecture та містить наступні шари:

DAL (Data Access Layer) - Шар доступу до даних
BLL (Business Logic Layer) - Шар бізнес-логіки
PL (Presentation Layer) - ASP.NET Core Web API
UI - ASP.NET Core MVC клієнт
Tests - Модульні тести

🚀 Основні функції
Для користувачів:

📚 Перегляд каталогу книг
🔍 Пошук та фільтрація книг за жанром, типом, автором
📖 Замовлення доступних книг
📋 Перегляд власних замовлень
↩️ Повернення книг

Для менеджерів:

➕ Додавання нових книг
✏️ Редагування інформації про книги
📊 Управління доступністю книг
👥 Перегляд всіх замовлень

Для адміністраторів:

🗑️ Видалення книг
👤 Управління користувачами
🔐 Управління ролями та правами доступу
🏗️ Створення нових адміністраторів та менеджерів

🛠️ Технології
Backend (Web API):

ASP.NET Core 8.0 - основний фреймворк
Entity Framework Core - ORM для роботи з базою даних
SQL Server - база даних
ASP.NET Core Identity - автентифікація та авторизація
JWT токени - для API автентифікації
AutoMapper - маппінг між моделями
Swagger/OpenAPI - документація API

Frontend (MVC):

ASP.NET Core MVC - веб-фреймворк
Bootstrap 5 - CSS фреймворк
jQuery - JavaScript бібліотека
Razor Pages - шаблонізатор

Тестування:

NUnit - фреймворк для тестування
Moq - бібліотека для моків
AutoMapper - в тестах

Патерни та принципи:

Repository Pattern - для доступу до даних
Unit of Work - для управління транзакціями
Dependency Injection - для інверсії залежностей
SOLID принципи

📁 Структура проекту
📦 Library-Management-System
├── 📂 DAL (Data Access Layer)
│   ├── 📂 DbContext
│   │   ├── LibraryDbContext.cs
│   │   └── LibraryDbContextFactory.cs
│   ├── 📂 Models
│   │   ├── ApplicationUser.cs
│   │   ├── ApplicationRole.cs
│   │   ├── Book.cs
│   │   └── Order.cs
│   ├── 📂 Enums
│   │   ├── BookTypes.cs
│   │   ├── GenreTypes.cs
│   │   └── OrderStatusTypes.cs
│   ├── 📂 Patterns
│   │   ├── Repository/
│   │   └── UnitOfWork/
│   └── 📂 Migrations
├── 📂 BLL (Business Logic Layer)
│   ├── 📂 Interfaces
│   │   ├── IBookService.cs
│   │   ├── IOrderService.cs
│   │   └── IUserService.cs
│   ├── 📂 Services
│   │   ├── BookService.cs
│   │   ├── OrderService.cs
│   │   └── UserService.cs
│   └── 📂 Exceptions
├── 📂 PL (Web API)
│   ├── 📂 Controllers
│   │   ├── BooksController.cs
│   │   ├── OrdersController.cs
│   │   ├── AccountController.cs
│   │   └── AdminUsersController.cs
│   ├── Program.cs
│   └── SeedDemoData.cs
├── 📂 UI (MVC Client)
│   ├── 📂 Controllers
│   │   ├── BaseController.cs
│   │   ├── BooksController.cs
│   │   ├── OrdersController.cs
│   │   ├── AccountController.cs
│   │   └── AdminController.cs
│   ├── 📂 Models
│   │   ├── DTOs/
│   │   └── ViewModels/
│   ├── 📂 Services
│   │   ├── ApiService.cs
│   │   └── IApiService.cs
│   └── 📂 Views
├── 📂 Mapping
│   ├── 📂 DTOs
│   └── 📂 Mapping
└── 📂 Tests
    ├── 📂 Services
    ├── 📂 Mocks
    └── 📂 TestHelpers
