# Library Management System

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9.0-512BD4)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927)
![Nginx](https://img.shields.io/badge/Nginx-Reverse%20Proxy-009639)
![JWT](https://img.shields.io/badge/Auth-JWT-orange)
![Ocelot](https://img.shields.io/badge/API%20Gateway-Ocelot-blue)


A production-ready library management system built with microservices architecture, featuring JWT authentication, Docker containerization, and modern .NET practices.

> **Note**: This is a portfolio project demonstrating enterprise-level software architecture and development practices. **Not currently deployed** - deployment instructions available for local and production environments.

##  Project Overview

This application showcases a complete microservices-based library system with separated concerns, scalable architecture, and production-ready features including API gateway, rate limiting, and containerization.

### Key Features

- **Microservices Architecture** - Independently deployable services with clear boundaries
- **JWT Authentication** - Secure token-based authentication with refresh tokens
- **API Gateway** - Centralized routing with Ocelot
- **Docker Support** - Full containerization with docker-compose
- **Rate Limiting** - API protection against abuse
- **Role-Based Access Control** - Administrator, Manager, and User roles
- **Reverse Proxy** - Nginx for load balancing and SSL termination
- **Comprehensive Testing** - Unit tests with NUnit and Moq

##  Architecture

### System Components

```
┌─────────────┐
│   Nginx     │ ← Reverse Proxy (Port 80/443)
└──────┬──────┘
       │
┌──────▼──────────────────────────────────────┐
│           API Gateway (Ocelot)              │ ← Port 5003
│        - Routing & Rate Limiting            │
└──────┬──────────────────────────────────────┘
       │
       ├─────────┬─────────┬─────────┬─────────┐
       │         │         │         │         │
   ┌───▼───┐ ┌──▼──┐  ┌──▼──┐  ┌───▼───┐ ┌──▼──┐
   │Account│ │Books│  │Order│  │ Admin │ │ UI  │
   │ :5010 │ │:5001│  │:5000│  │ :5005 │ │:8080│
   └───┬───┘ └──┬──┘  └──┬──┘  └───┬───┘ └─────┘
       │        │        │         │
       └────────┴────────┴─────────┘
                    │
              ┌─────▼─────┐
              │ SQL Server│
              │   :1433   │
              └───────────┘
```

### Microservices

| Service | Port | Responsibility | Technologies |
|---------|------|----------------|--------------|
| **API Gateway** | 5003 | Request routing, rate limiting, authentication validation | Ocelot, ASP.NET Core |
| **UI Service** | 8080 | Web interface, user interactions | ASP.NET Core MVC, Bootstrap |
| **Account Service** | 5010 | Authentication, JWT token management, user registration | ASP.NET Core Identity, JWT |
| **Books Service** | 5001 | Book catalog management, search, filtering | ASP.NET Core Web API |
| **Orders Service** | 5000 | Order processing, book reservations | ASP.NET Core Web API |
| **Admin Service** | 5005 | User management, role assignment | ASP.NET Core Web API |
| **Nginx** | 80/443 | Reverse proxy, SSL termination, load balancing | Nginx Alpine |
| **SQL Server** | 1433 | Persistent data storage | MS SQL Server 2022 |

### Technology Stack

**Backend**
- .NET 9.0
- Entity Framework Core 9.0
- ASP.NET Core Identity
- Ocelot API Gateway
- AutoMapper
- JWT Bearer Authentication

**Frontend**
- ASP.NET Core MVC
- Bootstrap 5
- jQuery
- Font Awesome

**Infrastructure**
- Docker & Docker Compose
- Nginx (Reverse Proxy)
- SQL Server 2022

**Testing**
- NUnit
- Moq
- Coverlet (Code Coverage)

##  Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for containerized deployment)
- [SQL Server](https://www.microsoft.com/sql-server) or Docker SQL Server image

### Option 1: Docker Deployment (Recommended)

**Quick start** - entire system with one command:

```bash
# Clone the repository
git clone https://github.com/yourusername/library-management-system.git
cd library-management-system

# Start all services
docker-compose up -d

# Access the application
# Web UI: http://localhost
# API Gateway: http://localhost:5003
```

**Services startup order:**
1. SQL Server (waits for health check)
2. Account Service (applies migrations, seeds data)
3. Other microservices (Books, Orders, Admin)
4. API Gateway (routes to all services)
5. UI & Nginx (public entry point)

### Option 2: Local Development

**1. Database Setup**
```bash
cd PI-223-1-7
dotnet ef database update
```

**2. Start Services** (each in separate terminal)

```bash
# Account Service (must start first - handles migrations)
cd AccountController
dotnet run --urls="http://localhost:5010"

# API Gateway
cd ApiGateway
dotnet run --urls="https://localhost:5003"

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

**3. Access Application**
- Web Interface: https://localhost:7280
- API Gateway: https://localhost:5003
- Swagger (if enabled): https://localhost:5001/swagger

### Default Credentials

The system automatically creates test users:

| Role | Email | Password | Permissions |
|------|-------|----------|-------------|
| **Administrator** | admin@example.com | Admin123 | Full system access |
| **Manager** | manager@example.com | Manager123 | Manage books and orders |
| **User** | user1@example.com | User123! | View books, create orders |

>  **Security Warning**: Change these credentials in production! Update in `AccountController/Controllers/RoleInitializer.cs` and `AccountController/SeedDemoData.cs`

##  Security Features

### Authentication & Authorization

**JWT Token-Based Authentication**
- Access tokens (60 min expiry)
- Refresh tokens (7 day expiry)
- Secure token storage in HTTP-only session
- Automatic token refresh middleware

**Configuration** (appsettings.json):
```json
{
  "JwtSettings": {
    "SecretKey": "YOUR-SECRET-KEY-HERE",  //  CHANGE THIS
    "Issuer": "LibraryApp",
    "Audience": "LibraryAppUsers",
    "ExpirationMinutes": 60
  }
}
```

### Rate Limiting

Protects against brute-force attacks and API abuse:

```csharp
// Authentication endpoints: 5 requests/minute
// General API: 100 requests/minute
// Global: 200 requests/minute per IP
```

### Role-Based Access Control

| Role | Capabilities |
|------|-------------|
| **Guest** | Browse catalog, search books |
| **RegisteredUser** | + Create orders, view own orders |
| **Manager** | + Manage catalog, view all orders |
| **Administrator** | + User management, role assignment |

## 📡 API Documentation

### Authentication Endpoints

```http
POST   /api/account/reg           # Register new user
POST   /api/account/log           # Login
POST   /api/account/logout        # Logout [Auth Required]
POST   /api/account/refresh-token # Refresh JWT token
GET    /api/account/me            # Get current user [Auth Required]
```

### Books API

```http
GET    /api/books/getall          # Get all books
GET    /api/books/getbyid/{id}    # Get book by ID
GET    /api/books/filter          # Filter books (genre, type, search)
POST   /api/books/createbook      # Create book [Manager+]
PUT    /api/books/updatebook/{id} # Update book [Manager+]
DELETE /api/books/delete/{id}     # Delete book [Admin]
POST   /api/books/orderbook/{id}  # Order a book [User+]
GET    /api/books/getuserorders   # Get user's ordered books [Auth]
```

### Orders API

```http
GET    /api/orders/getall         # Get all orders [Manager+]
GET    /api/orders/findspecific/{id} # Get specific order [Auth]
POST   /api/orders/createnew      # Create order [User+]
PUT    /api/orders/update         # Update order [Manager+]
DELETE /api/orders/delete/{id}    # Delete order [Auth]
```

### Admin API

```http
GET    /api/users/getall          # Get all users [Admin]
GET    /api/users/getuserbyid     # Get user by ID [Admin/Manager]
POST   /api/users/createuser      # Create user [Admin]
PUT    /api/users/updateuser      # Update user [Admin]
DELETE /api/users/deleteuser      # Delete user [Admin]
POST   /api/users/assignrole      # Assign role to user [Admin]
POST   /api/users/removerole      # Remove role from user [Admin]
GET    /api/users/getallroles     # Get all available roles [Admin/Manager]
```

**Example Request:**

```bash
# Login
curl -X POST https://localhost:5003/api/account/log \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "Admin123"
  }'

# Response
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "base64-encoded-token",
  "expiresIn": 3600,
  "user": {
    "id": "user-id",
    "email": "admin@example.com",
    "roles": ["Administrator"]
  }
}

# Use token in subsequent requests
curl -X GET https://localhost:5003/api/books/getall \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

## 🗄️ Data Models

### Core Entities

**Book**
```csharp
public class Book
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public GenreTypes Genre { get; set; }      // Fiction, Science, History, etc.
    public BookTypes Type { get; set; }        // Physical, Digital, Audio
    public bool IsAvailable { get; set; }
    public DateTime Year { get; set; }
    public ICollection<Order> Orders { get; set; }
}
```

**Order**
```csharp
public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int BookId { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatusTypes Type { get; set; }  // Pending, Approved, Completed, Cancelled
    public Book Book { get; set; }
}
```

**ApplicationUser** (extends IdentityUser)
```csharp
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public ICollection<Order> Orders { get; set; }
}
```

##  Testing

### Running Tests

```bash
cd Tests
dotnet test --logger:"console;verbosity=detailed"

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Test Coverage

- **BookService**: CRUD operations, filtering, sorting, availability management
- **OrderService**: Order lifecycle, validation, business rules
- **UserService**: User management, authentication, role assignment

### Testing Technologies
- NUnit 4.3.2 - Testing framework
- Moq 4.20.72 - Mocking library
- Coverlet - Code coverage

##  Docker Configuration

### Environment Variables

**Production deployment** - set these environment variables:

```yaml
# Database
- SA_PASSWORD=YourStrong@Passw0rd  #  CHANGE THIS
- ConnectionStrings__DefaultConnection=Server=sqlserver;Database=LibraryDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;

# JWT Configuration
- JwtSettings__SecretKey=YOUR-LONG-RANDOM-SECRET-KEY  #  CHANGE THIS (min 32 chars)
- JwtSettings__Issuer=LibraryApp
- JwtSettings__Audience=LibraryAppUsers
```

### Service Health Checks

All services include health check endpoints:

```bash
# Check service health
curl http://localhost:5010/api/account/health  # Account Service
curl http://localhost/health                    # Nginx
```

### Docker Commands

```bash
# Build and start
docker-compose up -d --build

# View logs
docker-compose logs -f [service-name]

# Stop all services
docker-compose down

# Remove volumes ( deletes database)
docker-compose down -v
```

## Production Deployment

### SSL Setup (Optional)

The project includes SSL-ready nginx configuration. To enable HTTPS:

1. Obtain SSL certificates (e.g., Let's Encrypt)
2. Place certificates in `nginx/ssl/`
3. Switch nginx config: `cp nginx/nginx.ssl.conf nginx/nginx.conf`
4. Update `docker-compose.yml` to mount SSL volume

Example with Let's Encrypt:
```bash
# On your server
chmod +x scripts/setup-ssl.sh
./scripts/setup-ssl.sh
```

### DuckDNS Setup (Free Dynamic DNS)

For home/lab hosting:
```bash
chmod +x scripts/update-duckdns.sh
./scripts/update-duckdns.sh
```

##  Project Structure

```
Library-Management-System/
├── AccountController/        # Authentication & JWT service
├── AdminUserService/         # User management service
├── ApiGateway/              # Ocelot API Gateway
├── BooksService/            # Books catalog service
├── OrdersService/           # Order management service
├── UI/                      # ASP.NET Core MVC web interface
├── PI-223-1-7 (DAL)/       # Data Access Layer
├── BLL/                     # Business Logic Layer
├── Mapping/                 # DTOs & AutoMapper profiles
├── Tests/                   # Unit tests
├── nginx/                   # Nginx configurations
├── scripts/                 # Deployment scripts
└── docker-compose.yml       # Container orchestration
```

##  Configuration

### API Gateway (Ocelot)

Routes are defined in `ApiGateway/ocelot.json` (local) and `ApiGateway/ocelot.Docker.json` (Docker):

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/Books/GetAll",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "books-service", "Port": 5001 }
      ],
      "UpstreamPathTemplate": "/api/books/getall",
      "UpstreamHttpMethod": ["GET"]
    }
  ]
}
```

### Database Migrations

Migrations are automatically applied on startup by Account Service. For manual control:

```bash
cd PI-223-1-7

# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback
dotnet ef database update PreviousMigrationName

# Remove last migration
dotnet ef migrations remove
```

##  Known Limitations & Future Enhancements

**Current Limitations:**
- No distributed caching (Redis) for scalability
- No message queue for async operations
- Single database instance (no read replicas)
- No circuit breaker pattern implementation

**Planned Features:**
- [ ] Redis caching layer
- [ ] Elasticsearch for advanced search
- [ ] GraphQL API alongside REST
- [ ] Kubernetes deployment manifests
- [ ] Monitoring with Prometheus/Grafana
- [ ] Distributed tracing with OpenTelemetry

##  Contributing

This is a portfolio project, but suggestions and feedback are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

##  License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

⭐ Star this repository if you find it helpful!
