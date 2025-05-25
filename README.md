# Library Management System

> [!NOTE]
> Система управління бібліотекою - це повнофункціональний веб-додаток для управління книгами, замовленнями та користувачами в бібліотеці. Проект реалізований з використанням ASP.NET Core Web API (бекенд) та ASP.NET Core MVC (фронтенд) з чистою архітектурою.

## Зміст

- [Архітектура проекту](#архітектура-проекту)
- [Основні функції](#основні-функції)
- [Технології](#технології)
- [Структура проекту](#структура-проекту)
- [Налаштування та запуск](#налаштування-та-запуск)
- [Тестування](#тестування)
- [API Документація](#api-документація)
- [Ролі та права доступу](#ролі-та-права-доступу)
- [Бізнес-логіка](#бізнес-логіка)
- [Конфігурація](#конфігурація)

## Архітектура проекту

Проект побудований на принципах **Clean Architecture** та містить наступні шари:

| Шар | Опис |
|-----|------|
| **DAL** | Data Access Layer - Шар доступу до даних |
| **BLL** | Business Logic Layer - Шар бізнес-логіки |
| **PL** | Presentation Layer - ASP.NET Core Web API |
| **UI** | ASP.NET Core MVC клієнт |
| **Tests** | Модульні тести |

## Основні функції

### :bust_in_silhouette: Для користувачів

- [x] Перегляд каталогу книг
- [x] Пошук та фільтрація книг за жанром, типом, автором
- [x] Замовлення доступних книг
- [x] Перегляд власних замовлень
- [x] Повернення книг

### :briefcase: Для менеджерів

- [x] Додавання нових книг
- [x] Редагування інформації про книги
- [x] Управління доступністю книг
- [x] Перегляд всіх замовлень

### :crown: Для адміністраторів

- [x] Видалення книг
- [x] Управління користувачами
- [x] Управління ролями та правами доступу
- [x] Створення нових адміністраторів та менеджерів

## Технології

<details>
<summary><strong>Backend (Web API)</strong></summary>

- **ASP.NET Core 9.0** - основний фреймворк
- **Entity Framework Core** - ORM для роботи з базою даних
- **SQL Server** - база даних
- **ASP.NET Core Identity** - автентифікація та авторизація
- **JWT токени** - для API автентифікації
- **AutoMapper** - маппінг між моделями
- **Swagger/OpenAPI** - документація API

</details>

<details>
<summary><strong>Frontend (MVC)</strong></summary>

- **ASP.NET Core MVC** - веб-фреймворк
- **Bootstrap 5** - CSS фреймворк
- **jQuery** - JavaScript бібліотека
- **Razor Pages** - шаблонізатор

</details>

<details>
<summary><strong>Тестування</strong></summary>

- **NUnit** - фреймворк для тестування
- **Moq** - бібліотека для моків
- **AutoMapper** - в тестах

</details>

<details>
<summary><strong>Патерни та принципи</strong></summary>

- **Repository Pattern** - для доступу до даних
- **Unit of Work** - для управління транзакціями
- **Dependency Injection** - для інверсії залежностей
- **SOLID принципи**

</details>

## Структура проекту

```
Library-Management-System/
│
├── DAL/                           # Data Access Layer
│   ├── DbContext/
│   │   ├── LibraryDbContext.cs
│   │   └── LibraryDbContextFactory.cs
│   ├── Models/
│   │   ├── ApplicationUser.cs
│   │   ├── ApplicationRole.cs
│   │   ├── Book.cs
│   │   └── Order.cs
│   ├── Enums/
│   │   ├── BookTypes.cs
│   │   ├── GenreTypes.cs
│   │   └── OrderStatusTypes.cs
│   ├── Patterns/
│   │   ├── Repository/
│   │   └── UnitOfWork/
│   └── Migrations/
│
├── BLL/                           # Business Logic Layer
│   ├── Interfaces/
│   │   ├── IBookService.cs
│   │   ├── IOrderService.cs
│   │   └── IUserService.cs
│   ├── Services/
│   │   ├── BookService.cs
│   │   ├── OrderService.cs
│   │   └── UserService.cs
│   └── Exceptions/
│
├── PL/                            # Web API
│   ├── Controllers/
│   │   ├── BooksController.cs
│   │   ├── OrdersController.cs
│   │   ├── AccountController.cs
│   │   └── AdminUsersController.cs
│   ├── Program.cs
│   └── SeedDemoData.cs
│
├── UI/                            # MVC Client
│   ├── Controllers/
│   │   ├── BaseController.cs
│   │   ├── BooksController.cs
│   │   ├── OrdersController.cs
│   │   ├── AccountController.cs
│   │   └── AdminController.cs
│   ├── Models/
│   │   ├── DTOs/
│   │   └── ViewModels/
│   ├── Services/
│   │   ├── ApiService.cs
│   │   └── IApiService.cs
│   └── Views/
│
├── Mapping/
│   ├── DTOs/
│   └── Mapping/
│
└── Tests/
    ├── Services/
    ├── Mocks/
    └── TestHelpers/
```

## Налаштування та запуск

### Передумови

> [!IMPORTANT]
> Переконайтеся, що у вас встановлено:
> - .NET 9.0 SDK
> - SQL Server (LocalDB або повна версія)
> - Visual Studio 2022 або VS Code

### Крок 1: Клонування репозиторію

```bash
git clone https://github.com/your-username/library-management-system.git
cd library-management-system
```

### Крок 2: Налаштування бази даних

1. Відкрийте `appsettings.json` в проекті **PL** та налаштуйте рядок підключення:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LibraryDb;Trusted_Connection=True;"
  }
}
```

2. Застосуйте міграції:

```bash
cd PL
dotnet ef database update
```

### Крок 3: Запуск API

```bash
cd PL
dotnet run
```

> [!NOTE]
> API буде доступний за адресою: https://localhost:7164

### Крок 4: Запуск MVC клієнта

```bash
cd UI
dotnet run
```

> [!NOTE]
> Веб-додаток буде доступний за адресою: https://localhost:7280

### Крок 5: Тестові дані

> [!TIP]
> При першому запуску автоматично створюються:
> - **Адміністратор**: `admin@example.com` / `Admin123`
> - **Менеджер**: `manager@example.com` / `Manager123`
> - Тестові книги та замовлення

## Тестування

Запуск усіх тестів:

```bash
cd Tests
dotnet test
```

Запуск з покриттям коду:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## API Документація

> [!IMPORTANT]
> Після запуску API, документація Swagger доступна за адресою: https://localhost:7164/swagger

### Основні endpoints

#### :books: Книги

| Метод | Endpoint | Опис | Права доступу |
|-------|----------|------|---------------|
| `GET` | `/api/books/GetAll` | Отримати всі книги | Всі |
| `GET` | `/api/books/GetById/{id}` | Отримати книгу за ID | Всі |
| `GET` | `/api/books/Filter` | Фільтрація книг | Всі |
| `POST` | `/api/books/Create` | Створити книгу | Manager+ |
| `PUT` | `/api/books/Update/{id}` | Оновити книгу | Manager+ |
| `DELETE` | `/api/books/Delete/{id}` | Видалити книгу | Admin |

#### :page_facing_up: Замовлення

| Метод | Endpoint | Опис | Права доступу |
|-------|----------|------|---------------|
| `GET` | `/Orders/Getall` | Всі замовлення | Manager+ |
| `GET` | `/Orders/GetSpecific/{id}` | Замовлення за ID | Manager+ |
| `POST` | `/Orders/CreateNewOrder` | Створити замовлення | User+ |
| `DELETE` | `/Orders/Delete` | Видалити замовлення | User+ |

#### :key: Автентифікація

| Метод | Endpoint | Опис |
|-------|----------|------|
| `POST` | `/api/account/register` | Реєстрація |
| `POST` | `/api/account/login` | Вхід |
| `POST` | `/api/account/logout` | Вихід |
| `GET` | `/api/account/status` | Статус автентифікації |

#### :shield: Адміністрування

| Метод | Endpoint | Опис |
|-------|----------|------|
| `GET` | `/api/AdminUsers/GetAllUsers` | Всі користувачі |
| `POST` | `/api/AdminUsers/CreateUser` | Створити користувача |
| `PUT` | `/api/AdminUsers/UpdateUser` | Оновити користувача |
| `DELETE` | `/api/AdminUsers/DeleteUser` | Видалити користувача |

## Ролі та права доступу

### :globe_with_meridians: Guest (неавторизований)

- Перегляд каталогу книг
- Пошук та фільтрація

### :bust_in_silhouette: RegisteredUser

> Усі права Guest +

- Замовлення книг
- Перегляд власних замовлень
- Повернення книг

### :briefcase: Manager

> Усі права RegisteredUser +

- Додавання нових книг
- Редагування існуючих книг
- Перегляд всіх замовлень
- Управління доступністю книг

### :crown: Administrator

> Усі права Manager +

- Видалення книг
- Управління користувачами
- Управління ролями
- Створення нових адміністраторів та менеджерів

## Бізнес-логіка

### Основні бізнес-правила

<details>
<summary><strong>Книги</strong></summary>

- Кожна книга має унікальний ID
- Книга може бути доступною або недоступною
- Недоступні книги не можна замовляти
- Книги з активними замовленнями не можна видаляти

</details>

<details>
<summary><strong>Замовлення</strong></summary>

- Користувач може замовити тільки доступну книгу
- Після замовлення книга стає недоступною
- Користувач може скасувати своє замовлення
- Після повернення книга знову стає доступною

</details>

<details>
<summary><strong>Користувачі</strong></summary>

- Кожен користувач має унікальний email
- Користувачі можуть мати кілька ролей
- Тільки адміністратори можуть управляти користувачами

</details>

## Конфігурація

### API Settings (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LibraryDb;Trusted_Connection=True;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "ExpiryDays": 7
  },
  "ApiSettings": {
    "BaseUrl": "https://localhost:7164"
  }
}
```

### MVC Client Settings

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7164"
  }
}
```

## Внесок у проект

1. Зробіть **Fork** репозиторію
2. Створіть гілку для нової функції:
   ```bash
   git checkout -b feature/new-feature
   ```
3. Зробіть коміт змін:
   ```bash
   git commit -am 'Add new feature'
   ```
4. Відправте зміни в гілку:
   ```bash
   git push origin feature/new-feature
   ```
5. Створіть **Pull Request**

:star: **Якщо цей проект був корисним, поставте зірочку на GitHub!**
