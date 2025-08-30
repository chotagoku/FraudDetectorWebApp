# FraudDetectorWebApp - Developer & Administrator Manual

**Version:** 1.0  
**Date:** 29 August 2025  
**Target Framework:** .NET 9.0  
**Database:** SQL Server with Entity Framework Core

---

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Installation & Setup](#2-installation--setup)
3. [Database Schema](#3-database-schema)
4. [API Reference](#4-api-reference)
5. [Configuration Management](#5-configuration-management)
6. [Background Services](#6-background-services)
7. [Security Implementation](#7-security-implementation)
8. [Deployment Guide](#8-deployment-guide)
9. [Monitoring & Maintenance](#9-monitoring--maintenance)
10. [Troubleshooting & Debugging](#10-troubleshooting--debugging)
11. [Performance Optimization](#11-performance-optimization)
12. [Extension Points](#12-extension-points)

---

## 1. System Architecture

### 1.1 High-Level Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Browser                            │
├─────────────────────────────────────────────────────────────┤
│  Razor Pages + Bootstrap UI + SignalR Client + JavaScript  │
└─────────────────┬───────────────────────────────────────────┘
                  │ HTTPS/WebSocket
┌─────────────────▼───────────────────────────────────────────┐
│                ASP.NET Core 9.0 Web Application             │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Razor Pages   │  │ API Controllers │  │ SignalR Hubs    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ Background      │  │ Entity Framework│  │ HTTP Client     │ │
│  │ Services        │  │ Core (ORM)      │  │ Factory         │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────┬───────────────────────────────────────────┘
                  │ SQL Connection
┌─────────────────▼───────────────────────────────────────────┐
│                SQL Server Database                          │
├─────────────────────────────────────────────────────────────┤
│  ApiConfigurations | GeneratedScenarios | ApiRequestLogs   │
└─────────────────────────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│              External APIs (Testing Targets)               │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 Technology Stack Details

#### Backend Framework
- **.NET 9.0**: Latest LTS version with improved performance
- **ASP.NET Core**: Web framework with built-in dependency injection
- **Entity Framework Core 9.0.8**: ORM for database operations
- **SignalR**: Real-time communication framework

#### Frontend Technologies
- **Razor Pages**: Server-side rendering with minimal JavaScript
- **Bootstrap 5.3.0**: Responsive CSS framework
- **Font Awesome**: Icon library
- **Vanilla JavaScript**: No heavy frontend frameworks

#### Data Layer
- **SQL Server**: Primary database (2019+ recommended)
- **Entity Framework Core**: Code-first migrations
- **Connection Pooling**: Built-in .NET connection management

### 1.3 Design Patterns Implemented

#### Patterns Used
- **Repository Pattern**: Implicit through Entity Framework DbContext
- **Dependency Injection**: Built-in ASP.NET Core DI container
- **Background Service Pattern**: For continuous API testing
- **Observer Pattern**: SignalR for real-time updates
- **Template Method Pattern**: Scenario generation algorithms

#### SOLID Principles Compliance
- **Single Responsibility**: Each controller/service has focused functionality
- **Open/Closed**: Extension points through interfaces and base classes
- **Liskov Substitution**: Interface implementations are substitutable
- **Interface Segregation**: Specific interfaces like `ISoftDelete`
- **Dependency Inversion**: Dependencies injected, not instantiated

---

## 2. Installation & Setup

### 2.1 Development Environment Setup

#### Prerequisites
```bash
# Required Software
.NET 9.0 SDK (latest version)
SQL Server 2019+ or SQL Server Express
Visual Studio 2022 or VS Code
Git (for version control)

# Optional Tools
SQL Server Management Studio (SSMS)
Postman or similar API testing tool
Azure Data Studio
```

#### Environment Verification
```powershell
# Check .NET version
dotnet --version
# Should return 9.0.x

# Check SQL Server connection
sqlcmd -S . -E
# Should connect without errors

# Verify Git
git --version
```

### 2.2 Project Setup

#### Clone and Restore
```bash
# Clone repository (adjust URL as needed)
git clone <repository-url>
cd FraudDetectorWebApp

# Restore NuGet packages
dotnet restore

# Build solution
dotnet build
```

#### Database Setup
```bash
# Update database (creates if doesn't exist)
dotnet ef database update

# Alternative: Create migration if needed
dotnet ef migrations add InitialCreate
dotnet ef database update
```

#### Configuration Setup
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=FraudDetectorApp;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

### 2.3 Running the Application

#### Development Mode
```bash
# Run with hot reload
dotnet run

# Or with specific environment
dotnet run --environment Development

# Watch mode for automatic restarts
dotnet watch run
```

#### Production Mode
```bash
# Build for production
dotnet build --configuration Release

# Run in production mode
dotnet run --configuration Release --environment Production
```

### 2.4 Docker Setup (Optional)

#### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["FraudDetectorWebApp.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FraudDetectorWebApp.dll"]
```

#### Docker Compose
```yaml
version: '3.8'
services:
  fraud-detector:
    build: .
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - sqlserver
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=FraudDetectorApp;User=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;
      
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong@Passw0rd"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

volumes:
  sqldata:
```

---

## 3. Database Schema

### 3.1 Entity Relationship Diagram

```
┌──────────────────────────┐     ┌──────────────────────────┐
│      ApiConfigurations   │◄────│    ApiRequestLogs        │
├──────────────────────────┤  1:N├──────────────────────────┤
│ Id (PK)                  │     │ Id (PK)                  │
│ Name                     │     │ ApiConfigurationId (FK)  │
│ ApiEndpoint              │     │ RequestPayload           │
│ RequestTemplate          │     │ ResponseContent          │
│ BearerToken              │     │ ResponseTimeMs           │
│ DelayBetweenRequests     │     │ StatusCode               │
│ MaxIterations            │     │ ErrorMessage             │
│ IsActive                 │     │ RequestTimestamp         │
│ TrustSslCertificate      │     │ IsSuccessful             │
│ CreatedAt                │     │ IterationNumber          │
│ UpdatedAt                │     │ IsDeleted                │
│ IsDeleted                │     │ DeletedAt                │
│ DeletedAt                │     └──────────────────────────┘
└──────────────────────────┘

┌──────────────────────────┐
│   GeneratedScenarios     │
├──────────────────────────┤
│ Id (PK)                  │
│ Name                     │
│ Description              │
│ ScenarioJson             │
│ RiskLevel                │
│ UserProfile              │
│ UserActivity             │
│ AmountRiskScore          │
│ AmountZScore             │
│ HighAmountFlag           │
│ HasWatchlistMatch        │
│ FromName                 │
│ ToName                   │
│ Amount                   │
│ ActivityCode             │
│ UserType                 │
│ GeneratedAt              │
│ [120+ additional fields] │
│ IsDeleted                │
│ DeletedAt                │
│ ConfigurationId          │
└──────────────────────────┘
```

### 3.2 Table Definitions

#### ApiConfigurations Table
```sql
CREATE TABLE ApiConfigurations (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(max) NOT NULL,
    ApiEndpoint nvarchar(max) NOT NULL,
    RequestTemplate nvarchar(max) NOT NULL,
    BearerToken nvarchar(max) NULL,
    DelayBetweenRequests int NOT NULL DEFAULT 5000,
    MaxIterations int NOT NULL DEFAULT 10,
    IsActive bit NOT NULL DEFAULT 0,
    TrustSslCertificate bit NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 NULL,
    IsDeleted bit NOT NULL DEFAULT 0,
    DeletedAt datetime2 NULL
);
```

#### GeneratedScenarios Table
```sql
CREATE TABLE GeneratedScenarios (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(max) NOT NULL,
    Description nvarchar(max) NOT NULL DEFAULT '',
    ScenarioJson nvarchar(max) NOT NULL,
    RiskLevel nvarchar(max) NOT NULL DEFAULT 'mixed',
    UserProfile nvarchar(max) NOT NULL DEFAULT '',
    UserActivity nvarchar(max) NOT NULL DEFAULT '',
    AmountRiskScore int NOT NULL,
    AmountZScore decimal(18,2) NOT NULL,
    HighAmountFlag bit NOT NULL,
    HasWatchlistMatch bit NOT NULL,
    FromName nvarchar(max) NOT NULL DEFAULT '',
    ToName nvarchar(max) NOT NULL DEFAULT '',
    Amount decimal(18,2) NOT NULL,
    ActivityCode nvarchar(max) NOT NULL DEFAULT '',
    UserType nvarchar(max) NOT NULL DEFAULT '',
    GeneratedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    ApiEndpoint nvarchar(max) NULL,
    IsTested bit NOT NULL DEFAULT 0,
    TestedAt datetime2 NULL,
    TestResponse nvarchar(max) NULL,
    ResponseTimeMs int NULL,
    TestSuccessful bit NULL,
    TestErrorMessage nvarchar(max) NULL,
    -- Additional 100+ fields for comprehensive fraud indicators
    ConfigurationId int NOT NULL DEFAULT 1,
    IsDeleted bit NOT NULL DEFAULT 0,
    DeletedAt datetime2 NULL
);
```

#### ApiRequestLogs Table
```sql
CREATE TABLE ApiRequestLogs (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ApiConfigurationId int NOT NULL,
    RequestPayload nvarchar(max) NOT NULL,
    ResponseContent nvarchar(max) NULL,
    ResponseTimeMs bigint NOT NULL,
    StatusCode int NOT NULL,
    ErrorMessage nvarchar(max) NULL,
    RequestTimestamp datetime2 NOT NULL DEFAULT GETUTCDATE(),
    IsSuccessful bit NOT NULL,
    IterationNumber int NOT NULL,
    IsDeleted bit NOT NULL DEFAULT 0,
    DeletedAt datetime2 NULL,
    
    CONSTRAINT FK_ApiRequestLogs_ApiConfigurations 
        FOREIGN KEY (ApiConfigurationId) REFERENCES ApiConfigurations(Id)
);
```

### 3.3 Indexes and Constraints

```sql
-- Performance indexes
CREATE INDEX IX_ApiRequestLogs_ConfigurationId ON ApiRequestLogs(ApiConfigurationId);
CREATE INDEX IX_ApiRequestLogs_Timestamp ON ApiRequestLogs(RequestTimestamp DESC);
CREATE INDEX IX_GeneratedScenarios_GeneratedAt ON GeneratedScenarios(GeneratedAt DESC);
CREATE INDEX IX_GeneratedScenarios_RiskLevel ON GeneratedScenarios(RiskLevel);
CREATE INDEX IX_GeneratedScenarios_ConfigurationId ON GeneratedScenarios(ConfigurationId);

-- Soft delete filters
CREATE INDEX IX_ApiConfigurations_IsDeleted ON ApiConfigurations(IsDeleted) WHERE IsDeleted = 0;
CREATE INDEX IX_GeneratedScenarios_IsDeleted ON GeneratedScenarios(IsDeleted) WHERE IsDeleted = 0;
CREATE INDEX IX_ApiRequestLogs_IsDeleted ON ApiRequestLogs(IsDeleted) WHERE IsDeleted = 0;
```

### 3.4 Entity Framework Configuration

#### DbContext Implementation
```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ApiConfiguration> ApiConfigurations { get; set; } = null!;
    public DbSet<GeneratedScenario> GeneratedScenarios { get; set; } = null!;
    public DbSet<ApiRequestLog> ApiRequestLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Global query filters for soft deletion
        modelBuilder.Entity<ApiConfiguration>()
            .HasQueryFilter(e => !e.IsDeleted);
        
        modelBuilder.Entity<GeneratedScenario>()
            .HasQueryFilter(e => !e.IsDeleted);
            
        modelBuilder.Entity<ApiRequestLog>()
            .HasQueryFilter(e => !e.IsDeleted);

        // Relationships
        modelBuilder.Entity<ApiRequestLog>()
            .HasOne(r => r.ApiConfiguration)
            .WithMany(c => c.RequestLogs)
            .HasForeignKey(r => r.ApiConfigurationId);

        // Default values
        modelBuilder.Entity<ApiConfiguration>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
```

### 3.5 Migration Management

#### Creating Migrations
```bash
# Add new migration
dotnet ef migrations add MigrationName

# Remove last migration
dotnet ef migrations remove

# Update database
dotnet ef database update

# Script migration for production
dotnet ef migrations script --output migration.sql
```

#### Migration Example
```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ApiConfigurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                // ... other columns
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiConfigurations", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ApiConfigurations");
    }
}
```

---

## 4. API Reference

### 4.1 API Controllers Overview

The application exposes REST APIs for external integration and internal functionality:

#### Base URL Structure
```
https://yourdomain.com/api/[controller]/[action]
```

#### Content Type
All APIs expect and return `application/json` unless specified otherwise.

### 4.2 Configuration Controller

#### GET /api/configuration
**Purpose**: Retrieve all API configurations  
**Parameters**: None  
**Returns**: Array of configuration objects

```json
// Response
[
  {
    "id": 1,
    "name": "Production Fraud API",
    "apiEndpoint": "https://api.example.com/fraud",
    "requestTemplate": "{ \"model\": \"fraud-detector\" }",
    "bearerToken": "token-value",
    "delayBetweenRequests": 5000,
    "maxIterations": 10,
    "isActive": true,
    "createdAt": "2025-08-29T10:00:00Z",
    "requestLogsCount": 150,
    "lastRequestTime": "2025-08-29T14:25:00Z"
  }
]
```

#### GET /api/configuration/{id}
**Purpose**: Retrieve specific configuration  
**Parameters**: 
- `id` (int): Configuration ID  
**Returns**: Configuration object or 404

#### POST /api/configuration
**Purpose**: Create new configuration  
**Body**: Configuration object
```json
{
  "name": "Test API Configuration",
  "apiEndpoint": "https://api.test.com/fraud",
  "requestTemplate": "{ \"model\": \"fraud-detector\", \"messages\": [] }",
  "bearerToken": "your-bearer-token",
  "delayBetweenRequests": 3000,
  "maxIterations": 50
}
```
**Returns**: Created configuration with assigned ID

#### PUT /api/configuration/{id}
**Purpose**: Update existing configuration  
**Parameters**: 
- `id` (int): Configuration ID  
**Body**: Updated configuration object  
**Returns**: 204 No Content on success

#### DELETE /api/configuration/{id}
**Purpose**: Delete configuration  
**Parameters**: 
- `id` (int): Configuration ID  
**Returns**: 204 No Content on success

#### POST /api/configuration/{id}/start
**Purpose**: Start testing for specific configuration  
**Parameters**: 
- `id` (int): Configuration ID  
**Returns**: Success message

#### POST /api/configuration/{id}/stop
**Purpose**: Stop testing for specific configuration  
**Parameters**: 
- `id` (int): Configuration ID  
**Returns**: Success message

#### POST /api/configuration/start-all
**Purpose**: Start all active configurations  
**Returns**: Success message

#### POST /api/configuration/stop-all
**Purpose**: Stop all configurations  
**Returns**: Success message

#### GET /api/configuration/status
**Purpose**: Get system testing status  
**Returns**: 
```json
{
  "isRunning": true
}
```

### 4.3 Generations Controller

#### GET /api/generations
**Purpose**: Retrieve generated scenarios with filtering and pagination  
**Parameters**: 
- `riskLevel` (string, optional): "low", "medium", "high", or "mixed"
- `configurationId` (int, optional): Filter by configuration
- `page` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Items per page (default: 25)

**Returns**: Array of scenario objects
```json
[
  {
    "id": 1,
    "name": "Small business owner - Bill Payment",
    "description": "HIGH risk scenario: Small business owner",
    "scenarioJson": "{ \"model\": \"fraud-detector:stable\", ... }",
    "riskLevel": "high",
    "userProfile": "Small business owner",
    "userActivity": "Today made 18 transactions",
    "amountRiskScore": 8,
    "amountZScore": 2.5,
    "highAmountFlag": true,
    "hasWatchlistMatch": false,
    "fromName": "AHMED TRADERS",
    "toName": "K-ELECTRIC",
    "amount": 750000,
    "activityCode": "Bill Payment",
    "userType": "MOBILE",
    "generatedAt": "2025-08-29T14:25:00Z",
    "isTested": true,
    "testedAt": "2025-08-29T14:30:00Z",
    "testResponse": "{ \"result\": \"flagged\" }",
    "responseTimeMs": 150,
    "testSuccessful": true,
    "testErrorMessage": null,
    "lastStatusCode": 200,
    "apiEndpoint": "https://api.example.com/fraud"
  }
]
```

#### GET /api/generations/{id}
**Purpose**: Retrieve specific scenario  
**Parameters**: 
- `id` (int): Scenario ID  
**Returns**: Detailed scenario object

#### POST /api/generations/generate
**Purpose**: Generate new scenarios  
**Body**: Generation request
```json
{
  "count": 5,
  "riskFocus": "high",
  "saveToDatabase": true,
  "format": "json"
}
```
**Returns**: Generated scenarios array

#### POST /api/generations/force-fresh
**Purpose**: Generate completely fresh scenarios (bypass database cache)  
**Body**: Generation request  
**Returns**: Freshly generated scenarios

#### POST /api/generations/{id}/test
**Purpose**: Test specific scenario against API  
**Parameters**: 
- `id` (int): Scenario ID  
**Body**: Test request
```json
{
  "apiEndpoint": "https://api.example.com/fraud",
  "bearerToken": "your-token-here"
}
```
**Returns**: Test results

#### POST /api/generations/bulk-test
**Purpose**: Test multiple scenarios  
**Body**: Bulk test request
```json
{
  "scenarioIds": [1, 2, 3, 4, 5],
  "apiEndpoint": "https://api.example.com/fraud",
  "bearerToken": "your-token-here"
}
```
**Returns**: Array of test results

#### GET /api/generations/random
**Purpose**: Get random scenario from database  
**Parameters**: 
- `riskLevel` (string, optional): Filter by risk level  
**Returns**: Random scenario object

#### GET /api/generations/statistics
**Purpose**: Get generation statistics  
**Returns**: Statistics object
```json
{
  "totalGenerated": 1250,
  "totalTested": 800,
  "successfulTests": 720,
  "averageResponseTime": 185.5,
  "riskDistribution": {
    "low": 400,
    "medium": 450,
    "high": 400
  }
}
```

#### GET /api/generations/favorites
**Purpose**: Get favorite scenarios  
**Returns**: Array of favorite scenarios

#### POST /api/generations/{id}/favorite
**Purpose**: Toggle scenario favorite status  
**Parameters**: 
- `id` (int): Scenario ID  
**Returns**: Updated favorite status

#### PUT /api/generations/{id}/notes
**Purpose**: Update scenario notes  
**Parameters**: 
- `id` (int): Scenario ID  
**Body**: Notes update request
```json
{
  "notes": "This scenario consistently triggers the watchlist filter"
}
```

#### DELETE /api/generations/{id}
**Purpose**: Soft delete scenario  
**Parameters**: 
- `id` (int): Scenario ID  
**Returns**: Success message

#### POST /api/generations/{id}/restore
**Purpose**: Restore soft-deleted scenario  
**Parameters**: 
- `id` (int): Scenario ID  
**Returns**: Success message

#### DELETE /api/generations/clear
**Purpose**: Soft delete all scenarios  
**Returns**: Count of deleted scenarios

#### GET /api/generations/deleted
**Purpose**: Get soft-deleted scenarios  
**Returns**: Array of deleted scenarios

### 4.4 Results Controller

#### GET /api/results
**Purpose**: Retrieve API test results  
**Parameters**: Various filtering options  
**Returns**: Array of result objects

#### GET /api/results/{id}
**Purpose**: Retrieve specific result  
**Parameters**: 
- `id` (int): Result ID  
**Returns**: Detailed result object

#### DELETE /api/results/configuration/{configId}
**Purpose**: Delete all results for configuration  
**Parameters**: 
- `configId` (int): Configuration ID  
**Returns**: Success message

### 4.5 Error Handling

All APIs return consistent error responses:

#### Success Response (200-299)
```json
{
  "data": { ... },
  "message": "Operation successful"
}
```

#### Error Response (400-599)
```json
{
  "error": "Error description",
  "message": "User-friendly error message",
  "statusCode": 400,
  "timestamp": "2025-08-29T14:25:00Z"
}
```

#### Common Status Codes
- **200 OK**: Successful GET request
- **201 Created**: Successful POST request
- **204 No Content**: Successful PUT/DELETE request
- **400 Bad Request**: Invalid request data
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Server error

### 4.6 Rate Limiting and Authentication

#### Current State
- **No authentication required** (recommendation: implement for production)
- **No rate limiting** (recommendation: add for API protection)
- **CORS enabled** for cross-origin requests

#### Recommended Implementation
```csharp
// Add to Program.cs for API authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options => {
        options.Authority = "https://your-identity-server.com";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

---

## 5. Configuration Management

### 5.1 Application Configuration

#### Configuration Sources (Priority Order)
1. **Command Line Arguments**: Highest priority
2. **Environment Variables**: Second priority  
3. **appsettings.{Environment}.json**: Environment-specific
4. **appsettings.json**: Base configuration
5. **User Secrets**: Development only

#### Key Configuration Sections

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=FraudDetectorApp;Integrated Security=true;TrustServerCertificate=true;"
  },
  "Application": {
    "Name": "FraudDetectorWebApp",
    "Version": "1.0.0",
    "DefaultDelayMs": 5000,
    "MaxScenarios": 1000,
    "EnableDetailedLogging": false
  }
}
```

### 5.2 Environment-Specific Configuration

#### Development (appsettings.Development.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=FraudDetectorApp_Dev;Integrated Security=true;TrustServerCertificate=true;"
  },
  "Application": {
    "EnableDetailedLogging": true,
    "MaxScenarios": 100
  }
}
```

#### Production (appsettings.Production.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=prod-server;Initial Catalog=FraudDetectorApp;Integrated Security=false;User ID=app_user;Password=***;TrustServerCertificate=false;"
  },
  "Application": {
    "EnableDetailedLogging": false,
    "MaxScenarios": 10000
  }
}
```

### 5.3 Secret Management

#### Development Secrets (User Secrets)
```bash
# Initialize user secrets
dotnet user-secrets init

# Set connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=FraudDetectorApp;Integrated Security=true;TrustServerCertificate=true;"

# Set API keys
dotnet user-secrets set "ExternalApi:BearerToken" "your-secret-token-here"

# List all secrets
dotnet user-secrets list
```

#### Production Secrets
**Recommended**: Use Azure Key Vault or similar secret management service

```csharp
// Add to Program.cs for Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

#### Environment Variables
```bash
# Set environment variables (Windows)
setx ASPNETCORE_ENVIRONMENT "Production"
setx ConnectionStrings__DefaultConnection "Server=prod;Database=FraudApp;..."

# Set environment variables (Linux/Mac)
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=prod;Database=FraudApp;..."
```

### 5.4 Configuration Validation

#### Strong-Typed Configuration
```csharp
// Configuration classes
public class ApplicationSettings
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int DefaultDelayMs { get; set; } = 5000;
    public int MaxScenarios { get; set; } = 1000;
    public bool EnableDetailedLogging { get; set; }
}

// Register in Program.cs
builder.Services.Configure<ApplicationSettings>(
    builder.Configuration.GetSection("Application"));

// Validation
builder.Services.AddOptions<ApplicationSettings>()
    .BindConfiguration("Application")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

#### Validation Attributes
```csharp
public class ApplicationSettings
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;
    
    [Required, RegularExpression(@"^\d+\.\d+\.\d+$")]
    public string Version { get; set; } = string.Empty;
    
    [Range(1000, 60000)]
    public int DefaultDelayMs { get; set; } = 5000;
    
    [Range(1, 100000)]
    public int MaxScenarios { get; set; } = 1000;
}
```

### 5.5 Dynamic Configuration Updates

#### Configuration Change Detection
```csharp
// Monitor configuration changes
public class ConfigurationMonitor : IHostedService
{
    private readonly IOptionsMonitor<ApplicationSettings> _optionsMonitor;
    private IDisposable? _optionsChangeToken;

    public ConfigurationMonitor(IOptionsMonitor<ApplicationSettings> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _optionsChangeToken = _optionsMonitor.OnChange(OnConfigurationChanged);
        return Task.CompletedTask;
    }

    private void OnConfigurationChanged(ApplicationSettings settings)
    {
        // Handle configuration changes
        Console.WriteLine($"Configuration updated: MaxScenarios = {settings.MaxScenarios}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _optionsChangeToken?.Dispose();
        return Task.CompletedTask;
    }
}
```

---

## 6. Background Services

### 6.1 ApiRequestService Architecture

The `ApiRequestService` is the core background service that handles continuous API testing.

#### Service Registration
```csharp
// Program.cs
builder.Services.AddSingleton<ApiRequestService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ApiRequestService>());
```

#### Service Lifecycle
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Application   │    │  Background      │    │   External      │
│   Starts        │───►│  Service         │───►│   APIs          │
│                 │    │  Starts          │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   User          │    │  Continuous      │    │   HTTP          │
│   Triggers      │◄───│  Processing      │───►│   Requests      │
│   Start/Stop    │    │  Loop            │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

### 6.2 Service Implementation Details

#### Core Processing Loop
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            if (_isRunning && _loopCancellationTokenSource != null && 
                !_loopCancellationTokenSource.Token.IsCancellationRequested)
            {
                await ProcessActiveConfigurations(_loopCancellationTokenSource.Token);
            }
            
            await Task.Delay(1000, stoppingToken); // Check every second
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApiRequestService execution");
            await Task.Delay(5000, stoppingToken); // Wait before retrying
        }
    }
}
```

#### Configuration Processing
```csharp
private async Task ProcessConfiguration(ApiConfiguration config, ApplicationDbContext context, CancellationToken cancellationToken)
{
    // Get current iteration count
    var currentIteration = await context.ApiRequestLogs
        .Where(l => l.ApiConfigurationId == config.Id)
        .CountAsync(cancellationToken) + 1;

    // Check iteration limits
    if (config.MaxIterations > 0 && currentIteration > config.MaxIterations)
    {
        config.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
        return;
    }

    // Prepare and execute request
    var requestPayload = PrepareRequestPayload(config.RequestTemplate, currentIteration);
    var requestLog = await ExecuteApiRequest(config, requestPayload, currentIteration, cancellationToken);
    
    // Save results
    context.ApiRequestLogs.Add(requestLog);
    await context.SaveChangesAsync(cancellationToken);
    
    // Notify clients via SignalR
    await NotifyNewResult(config, requestLog);
}
```

### 6.3 Request Payload Generation

#### Template Processing
The service supports dynamic placeholder replacement in request templates:

```csharp
private string PrepareRequestPayload(string template, int iteration)
{
    var payload = template;
    
    // Basic placeholders
    payload = payload.Replace("{{iteration}}", iteration.ToString());
    payload = payload.Replace("{{timestamp}}", DateTime.UtcNow.ToString("M/d/yyyy hh:mm:ss tt"));
    payload = payload.Replace("{{random}}", Random.Shared.Next(1000, 9999).ToString());
    
    // Financial data
    payload = payload.Replace("{{random_amount}}", Random.Shared.Next(10000, 999999).ToString());
    payload = payload.Replace("{{random_cnic}}", $"CN4210{Random.Shared.Next(100000000, 999999999)}");
    payload = payload.Replace("{{random_account}}", $"1063{Random.Shared.Next(100000000, 999999999)}");
    
    // Business data arrays (200+ predefined values)
    payload = payload.Replace("{{user_profile}}", GetRandomElement(UserProfiles));
    payload = payload.Replace("{{from_name}}", GetRandomElement(FromNames));
    payload = payload.Replace("{{to_name}}", GetRandomElement(ToNames));
    
    // Risk indicators
    payload = payload.Replace("{{amount_risk_score}}", Random.Shared.Next(1, 11).ToString());
    payload = payload.Replace("{{high_amount_flag}}", Random.Shared.NextDouble() > 0.6 ? "Yes" : "No");
    
    return payload;
}
```

#### Available Placeholders
| Category | Placeholders | Example Values |
|----------|-------------|----------------|
| **Basic** | `{{iteration}}`, `{{timestamp}}`, `{{random}}` | `1`, `8/29/2025 2:30 PM`, `7534` |
| **Financial** | `{{random_amount}}`, `{{random_cnic}}`, `{{random_iban}}` | `150000`, `CN421012345678`, `PK36HBL001234567890` |
| **Business** | `{{user_profile}}`, `{{from_name}}`, `{{to_name}}` | `Small business owner`, `AHMED TRADERS`, `K-ELECTRIC` |
| **Context** | `{{activity_code}}`, `{{user_type}}`, `{{transaction_comments}}` | `Bill Payment`, `MOBILE`, `Electricity Bill` |
| **Risk** | `{{amount_risk_score}}`, `{{watchlist_*}}` | `8`, `Yes`/`No` |

### 6.4 Error Handling and Resilience

#### HTTP Request Handling
```csharp
private async Task<ApiRequestLog> ExecuteApiRequest(ApiConfiguration config, string payload, int iteration, CancellationToken cancellationToken)
{
    var requestLog = new ApiRequestLog
    {
        ApiConfigurationId = config.Id,
        RequestPayload = payload,
        IterationNumber = iteration,
        RequestTimestamp = DateTime.UtcNow
    };

    var stopwatch = Stopwatch.StartNew();

    try
    {
        using var httpClient = CreateHttpClient(config);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync(config.ApiEndpoint, content, cancellationToken);
        
        stopwatch.Stop();
        requestLog.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        requestLog.StatusCode = (int)response.StatusCode;
        requestLog.IsSuccessful = response.IsSuccessStatusCode;

        if (response.IsSuccessStatusCode)
        {
            requestLog.ResponseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        else
        {
            requestLog.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
        }
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        requestLog.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
        requestLog.IsSuccessful = false;
        requestLog.ErrorMessage = ex.Message;
        requestLog.StatusCode = 0;
    }

    return requestLog;
}
```

#### SSL Certificate Handling
```csharp
private HttpClient CreateHttpClient(ApiConfiguration config)
{
    if (config.TrustSslCertificate)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage, cert, certChain, policyErrors) => true;
        
        var httpClient = new HttpClient(handler);
        _logger.LogWarning("SSL certificate validation bypassed for {ConfigName}", config.Name);
        return httpClient;
    }
    
    return _httpClientFactory.CreateClient();
}
```

### 6.5 Real-time Notifications

#### SignalR Integration
```csharp
private async Task NotifyNewResult(ApiConfiguration config, ApiRequestLog requestLog)
{
    try
    {
        var resultData = new
        {
            Id = requestLog.Id,
            Name = config.Name,
            ConfigurationId = config.Id,
            IterationNumber = requestLog.IterationNumber,
            RequestTimestamp = requestLog.RequestTimestamp,
            ResponseTimeMs = requestLog.ResponseTimeMs,
            IsSuccessful = requestLog.IsSuccessful,
            StatusCode = requestLog.StatusCode,
            ErrorMessage = requestLog.ErrorMessage
        };

        await _hubContext.Clients.Group("Dashboard").SendAsync("NewResult", resultData);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending SignalR notification");
    }
}
```

### 6.6 Performance Optimization

#### Resource Management
- **HttpClient Reuse**: Uses HttpClientFactory for connection pooling
- **Database Connections**: Scoped DbContext for each processing cycle
- **Memory Management**: Disposes resources properly
- **Cancellation Tokens**: Supports graceful shutdowns

#### Monitoring and Metrics
```csharp
// Add performance counters
private readonly Counter<int> _requestCounter;
private readonly Histogram<double> _responseTimeHistogram;

public ApiRequestService(IMeterFactory meterFactory)
{
    var meter = meterFactory.Create("FraudDetector.ApiTesting");
    _requestCounter = meter.CreateCounter<int>("api_requests_total");
    _responseTimeHistogram = meter.CreateHistogram<double>("api_response_time_ms");
}

// Record metrics
_requestCounter.Add(1, new TagList { {"config", config.Name}, {"success", requestLog.IsSuccessful} });
_responseTimeHistogram.Record(requestLog.ResponseTimeMs, new TagList { {"config", config.Name} });
```

---

## 7. Security Implementation

### 7.1 Current Security Measures

#### Input Validation
```csharp
// Model validation with DataAnnotations
public class ApiConfiguration
{
    [Required(ErrorMessage = "Configuration name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "API endpoint is required")]
    [Url(ErrorMessage = "Please provide a valid URL")]
    public string ApiEndpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "Request template is required")]
    public string RequestTemplate { get; set; } = string.Empty;

    [Range(1000, 300000, ErrorMessage = "Delay must be between 1 and 300 seconds")]
    public int DelayBetweenRequests { get; set; } = 5000;
}
```

#### HTTPS Enforcement
```csharp
// Program.cs - HTTPS redirection and HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}

app.UseHttpsRedirection();
```

#### CORS Configuration
```csharp
// Current CORS setup (needs hardening for production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    // More restrictive policy for SignalR
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins("http://localhost", "https://localhost")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

#### SQL Injection Protection
```csharp
// Entity Framework provides parameterized queries automatically
public async Task<List<GeneratedScenario>> GetScenariosByRiskLevel(string riskLevel)
{
    // This is automatically parameterized - safe from SQL injection
    return await _context.GeneratedScenarios
        .Where(s => s.RiskLevel == riskLevel)
        .ToListAsync();
}
```

### 7.2 Security Vulnerabilities & Fixes

#### HIGH PRIORITY: Plain Text Secrets

**Current Issue:**
```json
// appsettings.json - INSECURE
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;User ID=Trackeasy;Password=Trackeasy@123;..."
  }
}
```

**Recommended Fix:**
```csharp
// Use Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());

// Or use environment variables
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

#### HIGH PRIORITY: Bearer Token Storage

**Current Issue:**
```csharp
// Stored in plain text in database
public string? BearerToken { get; set; }
```

**Recommended Fix:**
```csharp
// Encrypt sensitive fields
public class ApiConfiguration
{
    [JsonIgnore]
    public string? BearerTokenEncrypted { get; set; }
    
    [NotMapped]
    public string? BearerToken 
    { 
        get => DecryptToken(BearerTokenEncrypted);
        set => BearerTokenEncrypted = EncryptToken(value);
    }
}

// Encryption service
public class TokenEncryptionService
{
    private readonly byte[] _key;
    
    public string EncryptToken(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);
        
        return Convert.ToBase64String(aes.IV.Concat(encrypted).ToArray());
    }
}
```

#### MEDIUM PRIORITY: CORS Policy Hardening

**Current Issue:**
```csharp
// Too permissive - allows any origin
policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
```

**Recommended Fix:**
```csharp
// Restrict to specific trusted domains
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
            "https://yourdomain.com",
            "https://app.yourdomain.com",
            "https://api.yourdomain.com"
        )
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Content-Type", "Authorization")
        .SetIsOriginAllowedToReturnTrue(); // Only if needed
    });
});
```

### 7.3 Authentication & Authorization

#### Recommended Authentication Setup

```csharp
// Add JWT authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://your-identity-server.com";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiUser", policy =>
        policy.RequireClaim("role", "api-user"));
        
    options.AddPolicy("Admin", policy =>
        policy.RequireClaim("role", "admin"));
});
```

#### Controller Protection
```csharp
[Authorize(Policy = "ApiUser")]
[ApiController]
[Route("api/[controller]")]
public class GenerationsController : ControllerBase
{
    [HttpPost("generate")]
    [Authorize(Policy = "Admin")] // Admin-only endpoint
    public async Task<ActionResult> GenerateScenarios([FromBody] GenerateRequest request)
    {
        // Implementation
    }
}
```

#### API Key Authentication (Alternative)
```csharp
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _validApiKey;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _validApiKey = configuration["ApiKey"] ?? throw new InvalidOperationException("API Key not configured");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) || 
                apiKey != _validApiKey)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }
        }

        await _next(context);
    }
}
```

### 7.4 Input Sanitization & Validation

#### Request Validation
```csharp
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Validate content length
        if (context.Request.ContentLength > 10_000_000) // 10MB limit
        {
            context.Response.StatusCode = 413;
            await context.Response.WriteAsync("Request too large");
            return;
        }

        // Validate content type for API endpoints
        if (context.Request.Path.StartsWithSegments("/api") &&
            context.Request.Method != "GET" &&
            !context.Request.ContentType?.StartsWith("application/json") == true)
        {
            context.Response.StatusCode = 415;
            await context.Response.WriteAsync("Unsupported media type");
            return;
        }

        await _next(context);
    }
}
```

#### Model Validation Enhancement
```csharp
public class ValidationActionFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            context.Result = new BadRequestObjectResult(new
            {
                message = "Validation failed",
                errors = errors
            });
        }
    }
}

// Register globally
builder.Services.Configure<MvcOptions>(options =>
{
    options.Filters.Add<ValidationActionFilter>();
});
```

### 7.5 Security Headers

#### Security Headers Middleware
```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("Permissions-Policy", "camera=(), microphone=(), location=()");
        
        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "img-src 'self' data:; " +
            "connect-src 'self' ws: wss:;");

        await _next(context);
    }
}

// Register in pipeline
app.UseMiddleware<SecurityHeadersMiddleware>();
```

### 7.6 Audit Logging

#### Security Audit Logger
```csharp
public class SecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly ApplicationDbContext _context;

    public async Task LogSecurityEvent(string eventType, string description, 
        string? userId = null, string? ipAddress = null)
    {
        var auditLog = new SecurityAuditLog
        {
            EventType = eventType,
            Description = description,
            UserId = userId,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        _context.SecurityAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Security Event: {EventType} - {Description}", eventType, description);
    }
}

// Usage in controllers
public class ConfigurationController : ControllerBase
{
    private readonly SecurityAuditService _auditService;

    [HttpPost]
    public async Task<ActionResult> CreateConfiguration(ApiConfiguration configuration)
    {
        // Create configuration logic...

        await _auditService.LogSecurityEvent(
            "ConfigurationCreated", 
            $"New API configuration created: {configuration.Name}",
            User.Identity?.Name,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return CreatedAtAction(nameof(GetConfiguration), new { id = configuration.Id }, configuration);
    }
}
```

### 7.7 Security Best Practices Checklist

#### Immediate Implementation (Priority 1)
- [ ] Move secrets to Azure Key Vault or environment variables
- [ ] Implement bearer token encryption in database
- [ ] Restrict CORS to specific trusted domains
- [ ] Add API authentication (JWT or API keys)
- [ ] Implement request validation middleware

#### Short-term Implementation (Priority 2)
- [ ] Add security headers middleware
- [ ] Implement rate limiting
- [ ] Add audit logging for security events
- [ ] Enable detailed security logging
- [ ] Implement input sanitization

#### Long-term Implementation (Priority 3)
- [ ] Add role-based authorization
- [ ] Implement OAuth 2.0 integration
- [ ] Add Web Application Firewall (WAF)
- [ ] Implement intrusion detection
- [ ] Add security monitoring dashboard

---

## 8. Deployment Guide

### 8.1 Deployment Architecture Options

#### Option 1: Single Server Deployment
```
┌─────────────────────────────────────────────────┐
│                Single Server                    │
├─────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────────────┐ │
│  │   IIS/Kestrel   │  │     SQL Server          │ │
│  │   Web Server    │  │     Database            │ │
│  └─────────────────┘  └─────────────────────────┘ │
└─────────────────────────────────────────────────┘
```

#### Option 2: Multi-Tier Deployment
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Load Balancer │    │   Web Servers   │    │   Database      │
│   (NGINX/ALB)   │───►│   (Multiple)    │───►│   Server        │
│                 │    │                 │    │   (Separate)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

#### Option 3: Cloud Deployment (Azure)
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Azure App      │    │  Azure SQL      │    │  Azure Key      │
│  Service        │───►│  Database       │    │  Vault          │
│                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 8.2 Windows Server Deployment (IIS)

#### Prerequisites
```powershell
# Install required Windows features
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpRedirect
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationDevelopment
Enable-WindowsOptionalFeature -Online -FeatureName IIS-NetFxExtensibility45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HealthAndDiagnostics
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Security
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Performance
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerManagementTools
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-IIS6ManagementCompatibility
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Metabase
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45
```

#### Install .NET Runtime
```powershell
# Download and install .NET 9.0 Runtime
# https://dotnet.microsoft.com/download/dotnet/9.0
# Install both ASP.NET Core Runtime and .NET Runtime
```

#### Build and Publish
```bash
# Build for production
dotnet build --configuration Release

# Publish application
dotnet publish --configuration Release --output "./publish"

# Create deployment package
compress-archive -Path "./publish/*" -DestinationPath "FraudDetectorWebApp.zip"
```

#### IIS Configuration
```xml
<!-- web.config (created automatically by publish) -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\FraudDetectorWebApp.dll" 
                  stdoutLogEnabled="false" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

#### IIS Site Setup
```powershell
# Create application pool
New-WebAppPool -Name "FraudDetectorAppPool" -Force
Set-ItemProperty -Path "IIS:\AppPools\FraudDetectorAppPool" -Name processModel.identityType -Value ApplicationPoolIdentity
Set-ItemProperty -Path "IIS:\AppPools\FraudDetectorAppPool" -Name "managedRuntimeVersion" -Value ""

# Create website
New-Website -Name "FraudDetectorWebApp" -Port 80 -PhysicalPath "C:\inetpub\wwwroot\FraudDetectorWebApp" -ApplicationPool "FraudDetectorAppPool"

# Set permissions
$acl = Get-Acl "C:\inetpub\wwwroot\FraudDetectorWebApp"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$acl.SetAccessRule($accessRule)
Set-Acl "C:\inetpub\wwwroot\FraudDetectorWebApp" $acl
```

### 8.3 Linux Server Deployment

#### Ubuntu/Debian Setup
```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install .NET runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y aspnetcore-runtime-9.0

# Install NGINX (optional reverse proxy)
sudo apt install -y nginx

# Create application directory
sudo mkdir -p /var/www/frauddetector
sudo chown $USER:$USER /var/www/frauddetector
```

#### Systemd Service Configuration
```ini
# /etc/systemd/system/frauddetector.service
[Unit]
Description=Fraud Detector Web Application
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/var/www/frauddetector
ExecStart=/usr/bin/dotnet /var/www/frauddetector/FraudDetectorWebApp.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=frauddetector
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

#### NGINX Configuration
```nginx
# /etc/nginx/sites-available/frauddetector
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

#### Deploy and Start Services
```bash
# Copy published files
scp -r ./publish/* user@server:/var/www/frauddetector/

# Set permissions
sudo chown -R www-data:www-data /var/www/frauddetector
sudo chmod -R 755 /var/www/frauddetector

# Enable and start services
sudo systemctl daemon-reload
sudo systemctl enable frauddetector
sudo systemctl start frauddetector
sudo systemctl status frauddetector

# Configure NGINX
sudo ln -s /etc/nginx/sites-available/frauddetector /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 8.4 Docker Deployment

#### Multi-Stage Dockerfile
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["FraudDetectorWebApp.csproj", "."]
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install dependencies for SQL Server connection
RUN apt-get update && apt-get install -y curl

# Copy published app
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "FraudDetectorWebApp.dll"]
```

#### Docker Compose for Production
```yaml
version: '3.8'

services:
  fraud-detector:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: fraud-detector-app
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=FraudDetectorApp;User=sa;Password=${SA_PASSWORD};TrustServerCertificate=true;
    depends_on:
      sql-server:
        condition: service_healthy
    volumes:
      - ./logs:/app/logs
    networks:
      - fraud-detector-network
    restart: unless-stopped
    
  sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: fraud-detector-sql
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SA_PASSWORD:-YourStrong@Passw0rd}
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sql-data:/var/opt/mssql
    networks:
      - fraud-detector-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P ${SA_PASSWORD} -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 3s
      retries: 10
      start_period: 10s
      
  nginx:
    image: nginx:alpine
    container_name: fraud-detector-nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - fraud-detector
    networks:
      - fraud-detector-network
    restart: unless-stopped

networks:
  fraud-detector-network:
    driver: bridge

volumes:
  sql-data:
    driver: local
```

#### Environment Configuration
```bash
# .env file
SA_PASSWORD=YourStrong@Passw0rd123!
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

### 8.5 Azure App Service Deployment

#### Azure Resources Setup
```bash
# Create resource group
az group create --name FraudDetector-RG --location "East US"

# Create App Service plan
az appservice plan create --name FraudDetector-Plan --resource-group FraudDetector-RG --sku S1 --is-linux

# Create web app
az webapp create --resource-group FraudDetector-RG --plan FraudDetector-Plan --name fraud-detector-app --runtime "DOTNETCORE:9.0"

# Create SQL Database
az sql server create --name fraud-detector-sql --resource-group FraudDetector-RG --location "East US" --admin-user dbadmin --admin-password "YourStrong@Password123!"
az sql db create --resource-group FraudDetector-RG --server fraud-detector-sql --name FraudDetectorApp --service-objective S0

# Configure connection strings
az webapp config connection-string set --resource-group FraudDetector-RG --name fraud-detector-app --connection-string-type SQLServer --settings DefaultConnection="Server=tcp:fraud-detector-sql.database.windows.net,1433;Initial Catalog=FraudDetectorApp;Persist Security Info=False;User ID=dbadmin;Password=YourStrong@Password123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

#### GitHub Actions Deployment
```yaml
# .github/workflows/deploy-azure.yml
name: Deploy to Azure App Service

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal
      
    - name: Publish
      run: dotnet publish --no-build --configuration Release --output ./publish
      
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'fraud-detector-app'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

### 8.6 Database Migration in Production

#### Pre-deployment Database Migration
```bash
# Generate SQL script for production
dotnet ef migrations script --output migration.sql --idempotent

# Review script before applying
# Apply manually or through deployment pipeline
```

#### Zero-Downtime Deployment Strategy
```bash
# 1. Deploy application with backward-compatible database changes
# 2. Run new version alongside old version
# 3. Gradually route traffic to new version
# 4. Remove old version after validation

# Blue-Green Deployment with Database
# 1. Create new database with migrations
# 2. Sync data from old to new database
# 3. Switch traffic to new environment
# 4. Verify and cleanup old environment
```

### 8.7 SSL/TLS Configuration

#### Let's Encrypt SSL (Linux)
```bash
# Install Certbot
sudo apt install certbot python3-certbot-nginx

# Obtain certificate
sudo certbot --nginx -d your-domain.com

# Auto-renewal (crontab)
echo "0 12 * * * /usr/bin/certbot renew --quiet" | sudo crontab -
```

#### Windows IIS SSL
```powershell
# Import certificate
Import-Certificate -FilePath "certificate.crt" -CertStoreLocation Cert:\LocalMachine\My

# Bind certificate to website
New-WebBinding -Name "FraudDetectorWebApp" -Protocol https -Port 443
```

### 8.8 Production Checklist

#### Pre-Deployment
- [ ] Database backup created
- [ ] Secrets moved to secure storage
- [ ] SSL certificates configured
- [ ] Environment variables set
- [ ] Firewall rules configured
- [ ] Health check endpoints tested
- [ ] Performance testing completed
- [ ] Security scanning passed

#### Post-Deployment
- [ ] Application starts successfully
- [ ] Database connection working
- [ ] All endpoints responding
- [ ] SSL certificate valid
- [ ] Logging functioning
- [ ] Monitoring alerts configured
- [ ] Backup procedures tested
- [ ] Recovery procedures documented

---

## 9. Monitoring & Maintenance

### 9.1 Application Monitoring

#### Built-in Logging
The application uses ASP.NET Core logging with structured logging:

```csharp
// Example logging in controllers
public class GenerationsController : ControllerBase
{
    private readonly ILogger<GenerationsController> _logger;
    
    public GenerationsController(ILogger<GenerationsController> logger)
    {
        _logger = logger;
    }
    
    [HttpPost("generate")]
    public async Task<ActionResult> GenerateScenarios([FromBody] GenerateRequest request)
    {
        _logger.LogInformation("Generating {Count} scenarios with risk focus: {RiskFocus}", 
            request.Count, request.RiskFocus);
            
        try
        {
            // Generation logic...
            _logger.LogInformation("Successfully generated {Count} scenarios", scenarios.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate scenarios for request: {@Request}", request);
            throw;
        }
    }
}
```

#### Log Configuration
```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "FraudDetectorWebApp": "Information"
    },
    "Console": {
      "LogLevel": {
        "Default": "Warning"
      }
    },
    "EventSource": {
      "LogLevel": {
        "Default": "Warning"
      }
    }
  }
}
```

#### Advanced Logging with Serilog
```csharp
// Program.cs - Add Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/fraud-detector-.txt", 
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.SQLServer(
            connectionString: context.Configuration.GetConnectionString("DefaultConnection"),
            sinkOptions: new SinkOptions
            {
                TableName = "ApplicationLogs",
                AutoCreateSqlTable = true
            });
});
```

### 9.2 Health Checks

#### Basic Health Check Implementation
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddDbContext<ApplicationDbContext>()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .AddUrlGroup(new Uri("https://api.example.com/health"), "external-api");

// Configure endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

#### Detailed Health Check
```csharp
public class FraudDetectorHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ApiRequestService _apiService;
    private readonly ILogger<FraudDetectorHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();

            // Check database connectivity
            var dbCanConnect = await _context.Database.CanConnectAsync(cancellationToken);
            data["database"] = dbCanConnect ? "healthy" : "unhealthy";

            // Check background service status
            data["background_service"] = _apiService.IsRunning ? "running" : "stopped";

            // Check recent performance
            var recentRequests = await _context.ApiRequestLogs
                .Where(r => r.RequestTimestamp > DateTime.UtcNow.AddMinutes(-5))
                .CountAsync(cancellationToken);
            data["recent_requests"] = recentRequests;

            // Check error rate
            var recentErrors = await _context.ApiRequestLogs
                .Where(r => r.RequestTimestamp > DateTime.UtcNow.AddMinutes(-5) && !r.IsSuccessful)
                .CountAsync(cancellationToken);
            var errorRate = recentRequests > 0 ? (double)recentErrors / recentRequests : 0;
            data["error_rate"] = $"{errorRate:P2}";

            if (!dbCanConnect)
                return HealthCheckResult.Unhealthy("Database connection failed", data: data);

            if (errorRate > 0.1) // 10% error rate threshold
                return HealthCheckResult.Degraded("High error rate detected", data: data);

            return HealthCheckResult.Healthy("All systems operational", data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Health check exception", ex);
        }
    }
}
```

### 9.3 Performance Monitoring

#### Application Insights (Azure)
```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Custom telemetry
public class TelemetryService
{
    private readonly TelemetryClient _telemetryClient;

    public void TrackScenarioGeneration(int count, string riskLevel, TimeSpan duration)
    {
        _telemetryClient.TrackEvent("ScenarioGenerated", 
            new Dictionary<string, string>
            {
                ["RiskLevel"] = riskLevel,
                ["Count"] = count.ToString()
            },
            new Dictionary<string, double>
            {
                ["Duration"] = duration.TotalMilliseconds
            });
    }

    public void TrackApiTest(string endpoint, bool success, double responseTime)
    {
        _telemetryClient.TrackDependency("HTTP", endpoint, "POST", DateTime.UtcNow.Subtract(TimeSpan.FromMilliseconds(responseTime)), TimeSpan.FromMilliseconds(responseTime), success);
    }
}
```

#### Custom Performance Counters
```csharp
public class PerformanceMetrics
{
    private readonly IMetrics _metrics;
    private readonly Counter<int> _scenariosGenerated;
    private readonly Counter<int> _apiRequestsTotal;
    private readonly Histogram<double> _apiResponseTime;

    public PerformanceMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("FraudDetector");
        _scenariosGenerated = meter.CreateCounter<int>("scenarios_generated_total");
        _apiRequestsTotal = meter.CreateCounter<int>("api_requests_total");
        _apiResponseTime = meter.CreateHistogram<double>("api_response_time_ms");
    }

    public void RecordScenarioGeneration(int count, string riskLevel)
    {
        _scenariosGenerated.Add(count, new TagList { { "risk_level", riskLevel } });
    }

    public void RecordApiRequest(string endpoint, bool success, double responseTimeMs)
    {
        _apiRequestsTotal.Add(1, new TagList 
        { 
            { "endpoint", endpoint }, 
            { "success", success.ToString() } 
        });
        
        _apiResponseTime.Record(responseTimeMs, new TagList { { "endpoint", endpoint } });
    }
}
```

### 9.4 Database Monitoring

#### Database Performance Queries
```sql
-- Monitor query performance
SELECT 
    TOP 10 
    qt.query_sql_text,
    qs.execution_count,
    qs.total_elapsed_time / 1000 as total_elapsed_time_ms,
    qs.avg_elapsed_time / 1000 as avg_elapsed_time_ms,
    qs.total_logical_reads,
    qs.avg_logical_reads
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.query_sql_text LIKE '%ApiRequestLogs%' OR qt.query_sql_text LIKE '%GeneratedScenarios%'
ORDER BY qs.avg_elapsed_time DESC;

-- Monitor database size growth
SELECT 
    name,
    size * 8 / 1024 as size_mb,
    FILEPROPERTY(name, 'SpaceUsed') * 8 / 1024 as used_mb,
    size * 8 / 1024 - FILEPROPERTY(name, 'SpaceUsed') * 8 / 1024 as free_mb
FROM sys.database_files;

-- Monitor active connections
SELECT 
    DB_NAME() as database_name,
    COUNT(*) as active_connections,
    status,
    login_name
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID()
GROUP BY status, login_name;
```

#### Index Performance Monitoring
```sql
-- Missing indexes
SELECT 
    migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) * (migs.user_seeks + migs.user_scans) as improvement_measure,
    'CREATE INDEX [IX_' + OBJECT_NAME(mid.object_id) + '_' + ISNULL(mid.equality_columns,'') + CASE WHEN mid.inequality_columns IS NOT NULL AND mid.equality_columns IS NOT NULL THEN '_' ELSE '' END + ISNULL(mid.inequality_columns, '') + ']' +
    ' ON ' + mid.statement + ' (' + ISNULL (mid.equality_columns,'') + CASE WHEN mid.inequality_columns IS NOT NULL AND mid.equality_columns IS NOT NULL THEN ',' ELSE '' END + ISNULL (mid.inequality_columns, '') + ')' + ISNULL (' INCLUDE (' + mid.included_columns + ')', '') as create_index_statement
FROM sys.dm_db_missing_index_groups mig
INNER JOIN sys.dm_db_missing_index_group_stats migs ON migs.group_handle = mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
WHERE migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) * (migs.user_seeks + migs.user_scans) > 10
ORDER BY migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans) DESC;
```

### 9.5 System Monitoring

#### Windows Performance Counters
```powershell
# CPU Usage
Get-Counter "\Processor(_Total)\% Processor Time"

# Memory Usage
Get-Counter "\Memory\Available MBytes"

# Disk I/O
Get-Counter "\PhysicalDisk(_Total)\Disk Reads/sec"
Get-Counter "\PhysicalDisk(_Total)\Disk Writes/sec"

# Network
Get-Counter "\Network Interface(*)\Bytes Total/sec"

# .NET Performance
Get-Counter "\.NET CLR Memory(w3wp)\% Time in GC"
Get-Counter "\.NET CLR Exceptions(w3wp)\# of Exceps Thrown / sec"
```

#### Linux System Monitoring
```bash
# System resources
htop
iostat -x 1
vmstat 1

# Application specific
journalctl -u frauddetector -f
netstat -tlnp | grep :5000

# Docker monitoring
docker stats fraud-detector-app
docker logs -f fraud-detector-app
```

### 9.6 Alerting & Notifications

#### Application Alerts
```csharp
public class AlertService
{
    private readonly ILogger<AlertService> _logger;
    private readonly IConfiguration _configuration;

    public async Task SendAlert(AlertLevel level, string title, string message, Dictionary<string, object>? data = null)
    {
        var alert = new Alert
        {
            Level = level,
            Title = title,
            Message = message,
            Timestamp = DateTime.UtcNow,
            Data = data ?? new Dictionary<string, object>()
        };

        // Log the alert
        _logger.Log(GetLogLevel(level), "ALERT: {Title} - {Message} {@Data}", title, message, data);

        // Send notifications based on configuration
        if (_configuration.GetValue<bool>("Alerts:Email:Enabled"))
        {
            await SendEmailAlert(alert);
        }

        if (_configuration.GetValue<bool>("Alerts:Slack:Enabled"))
        {
            await SendSlackAlert(alert);
        }

        if (level == AlertLevel.Critical)
        {
            await SendSmsAlert(alert);
        }
    }

    private LogLevel GetLogLevel(AlertLevel alertLevel) => alertLevel switch
    {
        AlertLevel.Info => LogLevel.Information,
        AlertLevel.Warning => LogLevel.Warning,
        AlertLevel.Error => LogLevel.Error,
        AlertLevel.Critical => LogLevel.Critical,
        _ => LogLevel.Information
    };
}
```

#### Monitoring Rules
```json
// Monitoring configuration
{
  "Monitoring": {
    "Thresholds": {
      "HighErrorRate": 0.10,
      "SlowResponseTime": 5000,
      "LowDiskSpace": 1024,
      "HighCpuUsage": 80,
      "HighMemoryUsage": 85
    },
    "CheckIntervals": {
      "HealthCheck": "00:01:00",
      "Performance": "00:05:00",
      "Resources": "00:10:00"
    }
  }
}
```

### 9.7 Maintenance Tasks

#### Daily Maintenance
```csharp
public class DailyMaintenanceService : IHostedService
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            // Clean up old logs (keep last 30 days)
            await CleanupOldLogs();
            
            // Archive old test results (keep last 90 days)
            await ArchiveOldResults();
            
            // Update database statistics
            await UpdateDatabaseStatistics();
            
            // Check for orphaned records
            await CleanupOrphanedRecords();
            
            // Generate daily summary report
            await GenerateDailySummary();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily maintenance failed");
        }
    }

    private async Task CleanupOldLogs()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var oldLogs = await _context.ApiRequestLogs
            .Where(l => l.RequestTimestamp < cutoffDate)
            .ToListAsync();

        _context.ApiRequestLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Cleaned up {Count} old log entries", oldLogs.Count);
    }
}
```

#### Database Maintenance
```sql
-- Daily database maintenance script
-- Update statistics
UPDATE STATISTICS ApiConfigurations;
UPDATE STATISTICS GeneratedScenarios;
UPDATE STATISTICS ApiRequestLogs;

-- Rebuild fragmented indexes
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql = @sql + 
    'ALTER INDEX ' + i.name + ' ON ' + SCHEMA_NAME(t.schema_id) + '.' + t.name + ' REBUILD;' + CHAR(13)
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps 
    ON i.object_id = ps.object_id AND i.index_id = ps.index_id
WHERE ps.avg_fragmentation_in_percent > 30
    AND i.type > 0;

EXEC sp_executesql @sql;

-- Check database integrity
DBCC CHECKDB WITH NO_INFOMSGS;
```

### 9.8 Backup & Recovery

#### Automated Database Backup
```sql
-- Daily full backup
BACKUP DATABASE FraudDetectorApp 
TO DISK = 'C:\Backups\FraudDetectorApp_Full.bak'
WITH FORMAT, INIT, COMPRESSION,
NAME = 'FraudDetectorApp Full Backup';

-- Hourly transaction log backup
BACKUP LOG FraudDetectorApp 
TO DISK = 'C:\Backups\FraudDetectorApp_Log.trn'
WITH COMPRESSION,
NAME = 'FraudDetectorApp Log Backup';
```

#### Application Files Backup
```powershell
# PowerShell backup script
$backupPath = "C:\Backups\FraudDetectorApp"
$sourcePath = "C:\inetpub\wwwroot\FraudDetectorWebApp"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Create backup directory
New-Item -Path "$backupPath\$timestamp" -ItemType Directory -Force

# Copy application files
Copy-Item -Path $sourcePath -Destination "$backupPath\$timestamp\App" -Recurse

# Copy configuration files
Copy-Item -Path "C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys" -Destination "$backupPath\$timestamp\Keys" -Recurse

# Create archive
Compress-Archive -Path "$backupPath\$timestamp" -DestinationPath "$backupPath\FraudDetectorApp_$timestamp.zip"

# Cleanup old backups (keep last 7 days)
Get-ChildItem -Path $backupPath -Name "*.zip" | 
Where-Object { $_.CreationTime -lt (Get-Date).AddDays(-7) } | 
Remove-Item -Force
```

### 9.9 Disaster Recovery

#### Recovery Procedures
```markdown
# Disaster Recovery Procedures

## Database Recovery
1. Restore latest full backup
2. Apply transaction log backups
3. Verify data integrity
4. Update connection strings

## Application Recovery
1. Install .NET runtime on new server
2. Restore application files from backup
3. Configure IIS/web server
4. Update configuration files
5. Start services and verify functionality

## Network Recovery
1. Configure DNS records
2. Set up SSL certificates
3. Configure firewall rules
4. Test connectivity

## Validation Checklist
- [ ] Application starts successfully
- [ ] Database connectivity verified
- [ ] All endpoints responding
- [ ] Background services running
- [ ] Monitoring and alerts configured
```

---

## 10. Troubleshooting & Debugging

### 10.1 Common Issues and Solutions

#### Application Won't Start

**Issue**: Application fails to start with database connection errors
```
Microsoft.Data.SqlClient.SqlException: A network-related or instance-specific error occurred
```

**Diagnostic Steps:**
```bash
# Check connection string
dotnet user-secrets list

# Test database connectivity
sqlcmd -S "your-server" -d "FraudDetectorApp" -E

# Verify .NET runtime
dotnet --list-runtimes
```

**Solutions:**
1. **Check SQL Server Service**: Ensure SQL Server is running
2. **Verify Connection String**: Check server name, database name, credentials
3. **Network Connectivity**: Test connectivity to database server
4. **Firewall Rules**: Ensure port 1433 is open
5. **SQL Server Configuration**: Enable TCP/IP protocol

#### High Memory Usage

**Issue**: Application consuming excessive memory
```
System.OutOfMemoryException: Insufficient memory to continue the execution
```

**Diagnostic Commands:**
```bash
# Windows
tasklist | findstr "FraudDetectorWebApp"
perfmon.exe

# Linux
ps aux | grep dotnet
htop
```

**Solutions:**
1. **Increase Server Memory**: Add more RAM
2. **Optimize Queries**: Reduce data loaded into memory
3. **Implement Pagination**: Limit result set sizes
4. **Clear Object References**: Proper disposal of resources
5. **Configure GC Settings**: Tune garbage collection

```csharp
// Optimize memory usage
builder.Services.Configure<GCSettings>(options =>
{
    options.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
});
```

#### SignalR Connection Issues

**Issue**: Real-time updates not working
```
Error: Connection disconnected with error 'Error: Server returned an error on close: Connection closed with an error.'
```

**Diagnostic Steps:**
```javascript
// Browser console debugging
connection.onclose(function (error) {
    console.error('SignalR connection closed:', error);
});

connection.onreconnecting(function (error) {
    console.log('SignalR attempting to reconnect:', error);
});
```

**Solutions:**
1. **Check WebSocket Support**: Enable WebSockets in IIS
2. **Configure CORS**: Allow SignalR origins
3. **Load Balancer Settings**: Enable sticky sessions
4. **Firewall Configuration**: Allow WebSocket traffic
5. **Check Network Proxies**: May block WebSocket connections

### 10.2 Debugging Configuration

#### Development Environment
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "Microsoft.AspNetCore.SignalR": "Debug"
    }
  },
  "DetailedErrors": true,
  "DebugMode": true
}
```

#### Debugging Tools Setup
```csharp
// Program.cs - Development debugging
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FraudDetector API V1");
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
```

### 10.3 Performance Debugging

#### Entity Framework Debugging
```csharp
// Enable EF Core logging
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseSqlServer(connectionString)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging() // Development only
        .EnableDetailedErrors();
}

// Query performance analysis
public async Task<List<GeneratedScenario>> GetScenariosOptimized(int page, int pageSize)
{
    using var activity = ActivitySource.StartActivity("GetScenarios");
    activity?.SetTag("page", page);
    activity?.SetTag("pageSize", pageSize);
    
    var stopwatch = Stopwatch.StartNew();
    
    var scenarios = await _context.GeneratedScenarios
        .AsNoTracking() // Read-only scenarios
        .Where(s => !s.IsDeleted)
        .OrderByDescending(s => s.GeneratedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(s => new GeneratedScenario // Project only needed fields
        {
            Id = s.Id,
            Name = s.Name,
            RiskLevel = s.RiskLevel,
            Amount = s.Amount,
            GeneratedAt = s.GeneratedAt
        })
        .ToListAsync();
    
    stopwatch.Stop();
    _logger.LogInformation("Retrieved {Count} scenarios in {ElapsedMs}ms", 
        scenarios.Count, stopwatch.ElapsedMilliseconds);
    
    return scenarios;
}
```

#### HTTP Client Debugging
```csharp
public class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHttpMessageHandler> _logger;

    public LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending {Method} request to {Uri}", 
            request.Method, request.RequestUri);

        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation("Received {StatusCode} response from {Uri} in {ElapsedMs}ms",
            response.StatusCode, request.RequestUri, stopwatch.ElapsedMilliseconds);

        return response;
    }
}

// Register in DI
builder.Services.AddTransient<LoggingHttpMessageHandler>();
builder.Services.AddHttpClient("fraud-api")
    .AddHttpMessageHandler<LoggingHttpMessageHandler>();
```

### 10.4 Error Analysis

#### Custom Exception Handling
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "An error occurred",
            message = exception.Message,
            statusCode = GetStatusCode(exception),
            timestamp = DateTime.UtcNow,
            path = context.Request.Path
        };

        context.Response.StatusCode = response.statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        ArgumentNullException => 400,
        ArgumentException => 400,
        KeyNotFoundException => 404,
        UnauthorizedAccessException => 401,
        NotImplementedException => 501,
        _ => 500
    };
}
```

#### Structured Error Logging
```csharp
public class ErrorDetails
{
    public string RequestId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

// Usage in controllers
[HttpPost("generate")]
public async Task<ActionResult> GenerateScenarios([FromBody] GenerateRequest request)
{
    var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    
    try
    {
        // Business logic...
    }
    catch (Exception ex)
    {
        var errorDetails = new ErrorDetails
        {
            RequestId = requestId,
            Method = HttpContext.Request.Method,
            Path = HttpContext.Request.Path,
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
            UserId = User.Identity?.Name ?? "Anonymous",
            AdditionalData = new Dictionary<string, object>
            {
                ["RequestBody"] = request,
                ["UserClaims"] = User.Claims.Select(c => new { c.Type, c.Value })
            }
        };

        _logger.LogError(ex, "Error generating scenarios: {@ErrorDetails}", errorDetails);
        throw;
    }
}
```

### 10.5 Database Troubleshooting

#### Connection Pool Issues
```csharp
// Monitor connection pool
public class ConnectionPoolMonitor
{
    private readonly ILogger<ConnectionPoolMonitor> _logger;
    private Timer? _timer;

    public void StartMonitoring()
    {
        _timer = new Timer(CheckConnectionPool, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private void CheckConnectionPool(object? state)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            
            connection.Open();
            
            using var command = new SqlCommand(@"
                SELECT 
                    counter_name,
                    cntr_value
                FROM sys.dm_os_performance_counters 
                WHERE counter_name LIKE '%Connection%'
                    AND instance_name = 'FraudDetectorApp'", connection);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                _logger.LogInformation("DB Connection: {CounterName} = {Value}",
                    reader["counter_name"], reader["cntr_value"]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check connection pool status");
        }
    }
}
```

#### Query Performance Analysis
```sql
-- Find slow queries
SELECT TOP 10
    qt.query_sql_text,
    qs.execution_count,
    qs.total_elapsed_time/1000 AS total_elapsed_time_ms,
    qs.avg_elapsed_time/1000 AS avg_elapsed_time_ms,
    qs.creation_time,
    qs.last_execution_time
FROM sys.dm_exec_query_stats AS qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS qt
WHERE qs.avg_elapsed_time > 1000000 -- More than 1 second average
ORDER BY qs.avg_elapsed_time DESC;

-- Find blocking queries
SELECT 
    r.session_id,
    r.blocking_session_id,
    r.wait_type,
    r.wait_time,
    r.command,
    s.login_name,
    s.host_name,
    t.text
FROM sys.dm_exec_requests r
INNER JOIN sys.dm_exec_sessions s ON r.session_id = s.session_id
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.blocking_session_id <> 0;
```

### 10.6 Performance Profiling

#### Application Performance Profiling
```csharp
public class PerformanceProfiler
{
    private readonly ActivitySource _activitySource;
    private readonly ILogger<PerformanceProfiler> _logger;

    public PerformanceProfiler(ILogger<PerformanceProfiler> logger)
    {
        _logger = logger;
        _activitySource = new ActivitySource("FraudDetector.Profiling");
    }

    public async Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation)
    {
        using var activity = _activitySource.StartActivity(operationName);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            activity?.SetTag("success", true);
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
            
            _logger.LogInformation("Operation {Operation} completed in {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            activity?.SetTag("success", false);
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("error", ex.Message);
            
            _logger.LogError(ex, "Operation {Operation} failed after {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}

// Usage
public async Task<List<GeneratedScenario>> GetScenarios(int page, int pageSize)
{
    return await _profiler.ProfileAsync("GetScenarios", async () =>
    {
        return await _context.GeneratedScenarios
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    });
}
```

### 10.7 Diagnostic Tools

#### Built-in Diagnostic Tools
```bash
# .NET diagnostic tools
dotnet-trace collect --process-id <pid>
dotnet-counters ps
dotnet-counters monitor --process-id <pid>
dotnet-dump collect --process-id <pid>

# Analysis
dotnet-trace convert trace.nettrace --format speedscope
dotnet-dump analyze dump.dmp
```

#### Custom Diagnostic Endpoint
```csharp
[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        var diagnostics = new
        {
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            WorkingSet = GC.GetTotalMemory(false),
            GCCollections = new
            {
                Gen0 = GC.CollectionCount(0),
                Gen1 = GC.CollectionCount(1),
                Gen2 = GC.CollectionCount(2)
            },
            Database = new
            {
                CanConnect = await _context.Database.CanConnectAsync(),
                PendingMigrations = (await _context.Database.GetPendingMigrationsAsync()).Count()
            },
            Cache = new
            {
                Type = _cache.GetType().Name,
                // Add cache statistics if available
            }
        };

        return Ok(diagnostics);
    }

    [HttpGet("config")]
    public ActionResult GetConfiguration()
    {
        var config = new
        {
            ConnectionStrings = Configuration.GetSection("ConnectionStrings")
                .GetChildren()
                .ToDictionary(x => x.Key, x => MaskConnectionString(x.Value)),
            Logging = Configuration.GetSection("Logging"),
            Environment = Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Where(e => e.Key.ToString()?.StartsWith("ASPNETCORE_") == true)
                .ToDictionary(e => e.Key, e => e.Value)
        };

        return Ok(config);
    }

    private static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return string.Empty;

        // Mask sensitive parts of connection string
        return Regex.Replace(connectionString, 
            @"(Password|Pwd)\s*=\s*[^;]+", 
            "$1=***", 
            RegexOptions.IgnoreCase);
    }
}
```

---

## 11. Performance Optimization

### 11.1 Database Optimization

#### Query Optimization

**Current Issues:**
```csharp
// Inefficient - loads all scenarios into memory
var scenarios = await _context.GeneratedScenarios.ToListAsync();
var filtered = scenarios.Where(s => s.RiskLevel == riskLevel).ToList();
```

**Optimized Version:**
```csharp
// Efficient - filters at database level
var scenarios = await _context.GeneratedScenarios
    .Where(s => s.RiskLevel == riskLevel)
    .AsNoTracking() // Read-only queries
    .Select(s => new GeneratedScenarioDto // Project only needed fields
    {
        Id = s.Id,
        Name = s.Name,
        RiskLevel = s.RiskLevel,
        Amount = s.Amount,
        GeneratedAt = s.GeneratedAt
    })
    .ToListAsync();
```

#### Index Optimization
```sql
-- Add covering indexes for common queries
CREATE NONCLUSTERED INDEX IX_GeneratedScenarios_RiskLevel_GeneratedAt 
ON GeneratedScenarios (RiskLevel, GeneratedAt DESC) 
INCLUDE (Id, Name, Amount);

CREATE NONCLUSTERED INDEX IX_ApiRequestLogs_ConfigurationId_Timestamp 
ON ApiRequestLogs (ApiConfigurationId, RequestTimestamp DESC) 
INCLUDE (IsSuccessful, ResponseTimeMs, StatusCode);

CREATE NONCLUSTERED INDEX IX_GeneratedScenarios_ConfigurationId_IsTested 
ON GeneratedScenarios (ConfigurationId, IsTested) 
INCLUDE (Id, Name, RiskLevel, TestSuccessful);

-- Filtered index for active configurations
CREATE NONCLUSTERED INDEX IX_ApiConfigurations_Active 
ON ApiConfigurations (IsActive) 
WHERE IsActive = 1;
```

#### Connection Pooling Optimization
```csharp
// Configure connection pooling in Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
}, ServiceLifetime.Scoped);

// Configure connection pool settings
builder.Services.Configure<SqlServerDbContextOptionsBuilder>(options =>
{
    options.MaxPoolSize = 100;
    options.MinPoolSize = 5;
});
```

### 11.2 Caching Strategies

#### Memory Caching
```csharp
public class ScenarioCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ScenarioCacheService> _logger;
    private readonly MemoryCacheEntryOptions _defaultOptions;

    public ScenarioCacheService(IMemoryCache cache, ApplicationDbContext context, ILogger<ScenarioCacheService> logger)
    {
        _cache = cache;
        _context = context;
        _logger = logger;
        
        _defaultOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Priority = CacheItemPriority.Normal
        };
    }

    public async Task<List<GeneratedScenario>> GetScenariosAsync(string cacheKey, Func<Task<List<GeneratedScenario>>> factory)
    {
        if (_cache.TryGetValue(cacheKey, out List<GeneratedScenario>? cachedScenarios))
        {
            _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
            return cachedScenarios!;
        }

        _logger.LogInformation("Cache miss for key: {CacheKey}", cacheKey);
        var scenarios = await factory();
        
        _cache.Set(cacheKey, scenarios, _defaultOptions);
        return scenarios;
    }

    public void InvalidateCache(string pattern)
    {
        // Implementation to remove cache entries matching pattern
        var field = typeof(MemoryCache).GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);
        var coherentState = field?.GetValue(_cache);
        var entriesCollection = coherentState?.GetType().GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
        var entries = (IDictionary?)entriesCollection?.GetValue(coherentState);

        if (entries == null) return;

        var keysToRemove = new List<object>();
        foreach (DictionaryEntry entry in entries)
        {
            if (entry.Key.ToString()?.Contains(pattern) == true)
            {
                keysToRemove.Add(entry.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        _logger.LogInformation("Invalidated {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
    }
}

// Usage in controllers
[HttpGet]
public async Task<ActionResult<List<GeneratedScenario>>> GetScenarios([FromQuery] string? riskLevel = null)
{
    var cacheKey = $"scenarios:{riskLevel ?? "all"}";
    
    var scenarios = await _cacheService.GetScenariosAsync(cacheKey, async () =>
    {
        var query = _context.GeneratedScenarios.AsQueryable();
        
        if (!string.IsNullOrEmpty(riskLevel))
        {
            query = query.Where(s => s.RiskLevel == riskLevel);
        }

        return await query
            .OrderByDescending(s => s.GeneratedAt)
            .Take(100)
            .ToListAsync();
    });

    return Ok(scenarios);
}
```

#### Distributed Caching (Redis)
```csharp
// Add Redis caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "FraudDetector";
});

public class RedisScenarioCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RedisScenarioCacheService> _logger;
    private readonly DistributedCacheEntryOptions _defaultOptions;

    public RedisScenarioCacheService(IDistributedCache cache, ApplicationDbContext context, ILogger<RedisScenarioCacheService> logger)
    {
        _cache = cache;
        _context = context;
        _logger = logger;
        
        _defaultOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(15)
        };
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var cachedValue = await _cache.GetStringAsync(key);
        if (cachedValue == null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(cachedValue);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize cached value for key: {Key}", key);
            await _cache.RemoveAsync(key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null) where T : class
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serializedValue, options ?? _defaultOptions);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    public async Task RemovePatternAsync(string pattern)
    {
        // Redis-specific implementation for pattern-based removal
        // This would require additional Redis functionality
    }
}
```

### 11.3 Background Processing Optimization

#### Optimized Background Service
```csharp
public class OptimizedApiRequestService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OptimizedApiRequestService> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<ConfigurationWork> _workQueue;

    public OptimizedApiRequestService(IServiceProvider serviceProvider, ILogger<OptimizedApiRequestService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount); // Limit concurrent requests
        _workQueue = new ConcurrentQueue<ConfigurationWork>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Producer: Queue configurations for processing
        var queueTask = Task.Run(async () => await QueueConfigurations(stoppingToken), stoppingToken);
        
        // Consumer: Process queued configurations
        var processTask = Task.Run(async () => await ProcessQueue(stoppingToken), stoppingToken);

        await Task.WhenAll(queueTask, processTask);
    }

    private async Task QueueConfigurations(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var activeConfigurations = await context.ApiConfigurations
                    .Where(c => c.IsActive)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                foreach (var config in activeConfigurations)
                {
                    if (ShouldProcessConfiguration(config))
                    {
                        _workQueue.Enqueue(new ConfigurationWork 
                        { 
                            Configuration = config, 
                            QueuedAt = DateTime.UtcNow 
                        });
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); // Check every 5 seconds
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queueing configurations");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    private async Task ProcessQueue(CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_workQueue.TryDequeue(out var work))
            {
                await _semaphore.WaitAsync(cancellationToken);
                
                var task = ProcessConfigurationAsync(work, cancellationToken)
                    .ContinueWith(t => _semaphore.Release(), TaskScheduler.Default);
                
                tasks.Add(task);
                
                // Clean up completed tasks
                tasks.RemoveAll(t => t.IsCompleted);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);
    }

    private async Task ProcessConfigurationAsync(ConfigurationWork work, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Check if configuration is still active
            var config = await context.ApiConfigurations
                .FirstOrDefaultAsync(c => c.Id == work.Configuration.Id && c.IsActive, cancellationToken);
                
            if (config == null) return;

            // Process the configuration
            await ProcessSingleConfiguration(config, context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing configuration {ConfigId}", work.Configuration.Id);
        }
    }

    private bool ShouldProcessConfiguration(ApiConfiguration config)
    {
        // Add logic to determine if configuration should be processed now
        // Consider delay between requests, last execution time, etc.
        return true; // Simplified for example
    }

    private class ConfigurationWork
    {
        public ApiConfiguration Configuration { get; set; } = null!;
        public DateTime QueuedAt { get; set; }
    }
}
```

### 11.4 HTTP Client Optimization

#### Optimized HTTP Client Configuration
```csharp
// Configure HTTP clients with optimal settings
builder.Services.AddHttpClient("fraud-api", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FraudDetector/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    MaxConnectionsPerServer = 10,
    UseCookies = false,
    UseProxy = false
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Retry policy
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
            });
}

// Circuit breaker policy
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (result, duration) =>
            {
                Console.WriteLine($"Circuit breaker opened for {duration}");
            },
            onReset: () =>
            {
                Console.WriteLine("Circuit breaker closed");
            });
}
```

### 11.5 Memory Management Optimization

#### Object Pool Usage
```csharp
public class ScenarioGenerationService
{
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;
    private readonly ObjectPool<JsonSerializerOptions> _jsonOptionsPool;

    public ScenarioGenerationService(ObjectPoolProvider poolProvider)
    {
        _stringBuilderPool = poolProvider.Create<StringBuilder>();
        _jsonOptionsPool = poolProvider.Create(new DefaultPooledObjectPolicy<JsonSerializerOptions>());
    }

    public string GenerateScenarioJson(GeneratedScenario scenario)
    {
        var jsonOptions = _jsonOptionsPool.Get();
        var stringBuilder = _stringBuilderPool.Get();

        try
        {
            // Use pooled objects for JSON generation
            jsonOptions.WriteIndented = true;
            jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            var json = JsonSerializer.Serialize(scenario, jsonOptions);
            return json;
        }
        finally
        {
            // Return objects to pool
            _jsonOptionsPool.Return(jsonOptions);
            _stringBuilderPool.Return(stringBuilder);
        }
    }
}

// Register object pools
builder.Services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
```

#### Span and Memory Usage
```csharp
public class OptimizedStringProcessor
{
    public ReadOnlySpan<char> ProcessTemplate(ReadOnlySpan<char> template, Dictionary<string, string> replacements)
    {
        Span<char> buffer = stackalloc char[template.Length * 2]; // Stack allocation for small buffers
        var writer = new SpanWriter(buffer);

        int lastIndex = 0;
        foreach (var kvp in replacements)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}".AsSpan();
            var index = template.IndexOf(placeholder);
            
            if (index >= 0)
            {
                // Copy text before placeholder
                writer.Write(template.Slice(lastIndex, index - lastIndex));
                // Write replacement
                writer.Write(kvp.Value.AsSpan());
                lastIndex = index + placeholder.Length;
            }
        }

        // Copy remaining text
        writer.Write(template.Slice(lastIndex));
        return writer.WrittenSpan;
    }
}

public ref struct SpanWriter
{
    private readonly Span<char> _buffer;
    private int _position;

    public SpanWriter(Span<char> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public void Write(ReadOnlySpan<char> text)
    {
        text.CopyTo(_buffer.Slice(_position));
        _position += text.Length;
    }

    public ReadOnlySpan<char> WrittenSpan => _buffer.Slice(0, _position);
}
```

### 11.6 Database Batch Processing

#### Bulk Operations
```csharp
public class BulkOperationService
{
    private readonly ApplicationDbContext _context;
    
    public async Task BulkInsertScenariosAsync(IEnumerable<GeneratedScenario> scenarios)
    {
        var batchSize = 1000;
        var scenarioList = scenarios.ToList();
        
        for (int i = 0; i < scenarioList.Count; i += batchSize)
        {
            var batch = scenarioList.Skip(i).Take(batchSize);
            
            _context.GeneratedScenarios.AddRange(batch);
            await _context.SaveChangesAsync();
            
            // Clear change tracker to free memory
            _context.ChangeTracker.Clear();
        }
    }

    public async Task BulkUpdateScenariosAsync(IEnumerable<GeneratedScenario> scenarios)
    {
        // Use raw SQL for bulk updates
        var scenarioIds = scenarios.Select(s => s.Id).ToList();
        
        await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE GeneratedScenarios 
            SET IsTested = 1, TestedAt = GETUTCDATE()
            WHERE Id IN ({0})", 
            string.Join(",", scenarioIds));
    }

    public async Task BulkDeleteOldRecordsAsync(DateTime cutoffDate)
    {
        // Use raw SQL for efficient bulk deletes
        await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE ApiRequestLogs 
            SET IsDeleted = 1, DeletedAt = GETUTCDATE()
            WHERE RequestTimestamp < {0} AND IsDeleted = 0",
            cutoffDate);

        // Physically delete very old soft-deleted records
        await _context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ApiRequestLogs 
            WHERE IsDeleted = 1 AND DeletedAt < {0}",
            cutoffDate.AddDays(-90)); // Delete after 90 days
    }
}
```

### 11.7 Frontend Performance Optimization

#### JavaScript Optimization
```javascript
// Efficient data handling with pagination
class ScenarioManager {
    constructor() {
        this.cache = new Map();
        this.currentPage = 1;
        this.pageSize = 25;
        this.isLoading = false;
    }

    async loadScenarios(page = 1, forceRefresh = false) {
        const cacheKey = `scenarios_${page}_${this.pageSize}`;
        
        if (!forceRefresh && this.cache.has(cacheKey)) {
            return this.cache.get(cacheKey);
        }

        if (this.isLoading) return;
        
        this.isLoading = true;
        try {
            const response = await fetch(`/api/generations?page=${page}&pageSize=${this.pageSize}`);
            const data = await response.json();
            
            // Cache the results
            this.cache.set(cacheKey, data);
            
            // Limit cache size to prevent memory issues
            if (this.cache.size > 50) {
                const firstKey = this.cache.keys().next().value;
                this.cache.delete(firstKey);
            }
            
            return data;
        } finally {
            this.isLoading = false;
        }
    }

    // Debounced search function
    debounceSearch = this.debounce(async (searchTerm) => {
        const response = await fetch(`/api/generations?search=${encodeURIComponent(searchTerm)}`);
        return response.json();
    }, 300);

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Virtual scrolling for large datasets
    setupVirtualScrolling(containerElement) {
        const itemHeight = 60; // Height of each scenario row
        const visibleItems = Math.ceil(containerElement.clientHeight / itemHeight) + 2;
        
        let scrollTop = 0;
        let startIndex = 0;
        
        containerElement.addEventListener('scroll', () => {
            scrollTop = containerElement.scrollTop;
            startIndex = Math.floor(scrollTop / itemHeight);
            
            this.renderVisibleItems(startIndex, visibleItems);
        });
    }

    renderVisibleItems(startIndex, count) {
        // Only render items that are currently visible
        // This prevents DOM overload with thousands of elements
        const endIndex = Math.min(startIndex + count, this.totalItems);
        
        // Update DOM with only visible items
        this.updateVisibleDOM(startIndex, endIndex);
    }
}

// Optimize SignalR connection
class OptimizedSignalRClient {
    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/apitest', {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect([0, 2000, 10000, 30000]) // Retry intervals
            .configureLogging(signalR.LogLevel.Warning) // Reduce logging in production
            .build();
        
        this.setupConnectionHandlers();
    }

    setupConnectionHandlers() {
        this.connection.onreconnecting((error) => {
            console.log('SignalR reconnecting:', error);
            this.showConnectionStatus('Reconnecting...');
        });

        this.connection.onreconnected(() => {
            console.log('SignalR reconnected');
            this.showConnectionStatus('Connected');
        });

        this.connection.onclose((error) => {
            console.log('SignalR connection closed:', error);
            this.showConnectionStatus('Disconnected');
        });
    }

    showConnectionStatus(status) {
        // Update UI to show connection status
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            statusElement.textContent = status;
            statusElement.className = `status ${status.toLowerCase()}`;
        }
    }
}
```

### 11.8 Performance Monitoring and Benchmarking

#### Benchmarking Service
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class ScenarioGenerationBenchmark
{
    private ApplicationDbContext _context;
    private ILogger<GenerationsController> _logger;
    private IHttpClientFactory _httpClientFactory;
    private GenerationsController _controller;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("BenchmarkDb")
            .Options;
        
        _context = new ApplicationDbContext(options);
        _logger = new NullLogger<GenerationsController>();
        _httpClientFactory = new MockHttpClientFactory();
        _controller = new GenerationsController(_context, _logger, _httpClientFactory);
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(10)]
    [Arguments(100)]
    public async Task GenerateScenarios(int count)
    {
        var request = new GenerateRequest { Count = count, SaveToDatabase = false };
        await _controller.GenerateScenarios(request);
    }

    [Benchmark]
    public async Task QueryScenarios()
    {
        await _controller.GetGenerations(riskLevel: null, configurationId: null, page: 1, pageSize: 25);
    }
}

// Performance testing utilities
public class PerformanceTestHelper
{
    public static async Task<PerformanceResult> MeasureAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        int iterations = 1)
    {
        var results = new List<TimeSpan>();
        var memoryBefore = GC.GetTotalMemory(true);

        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await operation();
            stopwatch.Stop();
            results.Add(stopwatch.Elapsed);
        }

        var memoryAfter = GC.GetTotalMemory(true);

        return new PerformanceResult
        {
            OperationName = operationName,
            Iterations = iterations,
            TotalTime = results.Aggregate(TimeSpan.Zero, (sum, time) => sum.Add(time)),
            AverageTime = TimeSpan.FromTicks(results.Sum(t => t.Ticks) / results.Count),
            MinTime = results.Min(),
            MaxTime = results.Max(),
            MemoryUsed = memoryAfter - memoryBefore
        };
    }
}

public class PerformanceResult
{
    public string OperationName { get; set; } = string.Empty;
    public int Iterations { get; set; }
    public TimeSpan TotalTime { get; set; }
    public TimeSpan AverageTime { get; set; }
    public TimeSpan MinTime { get; set; }
    public TimeSpan MaxTime { get; set; }
    public long MemoryUsed { get; set; }

    public override string ToString()
    {
        return $"{OperationName}: Avg {AverageTime.TotalMilliseconds:F2}ms, " +
               $"Min {MinTime.TotalMilliseconds:F2}ms, " +
               $"Max {MaxTime.TotalMilliseconds:F2}ms, " +
               $"Memory {MemoryUsed:N0} bytes";
    }
}
```

---

## 12. Extension Points

### 12.1 Plugin Architecture

#### Plugin Interface Definition
```csharp
// Core plugin interfaces
public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    Task InitializeAsync(IServiceProvider serviceProvider);
    Task ShutdownAsync();
}

public interface IScenarioGenerator : IPlugin
{
    Task<IEnumerable<GeneratedScenario>> GenerateScenariosAsync(
        ScenarioGenerationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IApiTestHandler : IPlugin
{
    Task<ApiTestResult> ExecuteTestAsync(
        ApiConfiguration configuration,
        GeneratedScenario scenario,
        CancellationToken cancellationToken = default);
}

public interface IResultProcessor : IPlugin
{
    Task<ProcessedResult> ProcessResultAsync(
        ApiRequestLog requestLog,
        CancellationToken cancellationToken = default);
}

// Plugin metadata
[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Type[] Dependencies { get; set; } = Array.Empty<Type>();
}
```

#### Plugin Discovery and Loading
```csharp
public class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly IServiceCollection _services;
    private readonly List<IPlugin> _loadedPlugins = new();

    public async Task<IEnumerable<IPlugin>> LoadPluginsAsync(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("Plugin directory does not exist: {Directory}", pluginDirectory);
            return Enumerable.Empty<IPlugin>();
        }

        var pluginAssemblies = Directory.GetFiles(pluginDirectory, "*.dll")
            .Select(Assembly.LoadFrom)
            .ToList();

        foreach (var assembly in pluginAssemblies)
        {
            try
            {
                await LoadPluginsFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugins from assembly: {Assembly}", assembly.FullName);
            }
        }

        return _loadedPlugins;
    }

    private async Task LoadPluginsFromAssembly(Assembly assembly)
    {
        var pluginTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IPlugin)) && !t.IsAbstract)
            .ToList();

        foreach (var pluginType in pluginTypes)
        {
            try
            {
                var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                var pluginAttribute = pluginType.GetCustomAttribute<PluginAttribute>();

                _logger.LogInformation("Loading plugin: {Name} v{Version} by {Author}",
                    pluginAttribute?.Name ?? pluginType.Name,
                    pluginAttribute?.Version ?? "Unknown",
                    pluginAttribute?.Author ?? "Unknown");

                // Register plugin services
                RegisterPluginServices(pluginType, plugin);

                // Initialize plugin
                await plugin.InitializeAsync(_services.BuildServiceProvider());
                
                _loadedPlugins.Add(plugin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin: {PluginType}", pluginType.Name);
            }
        }
    }

    private void RegisterPluginServices(Type pluginType, IPlugin plugin)
    {
        // Register the plugin itself
        _services.AddSingleton(plugin);

        // Register plugin interfaces
        foreach (var interfaceType in pluginType.GetInterfaces().Where(i => i != typeof(IPlugin)))
        {
            _services.AddSingleton(interfaceType, plugin);
        }
    }
}
```

#### Sample Plugin Implementation
```csharp
[Plugin(
    Name = "Advanced Scenario Generator",
    Version = "1.0.0",
    Author = "Plugin Developer",
    Description = "Generates scenarios using machine learning algorithms")]
public class AdvancedScenarioGeneratorPlugin : IScenarioGenerator
{
    public string Name => "Advanced Scenario Generator";
    public string Version => "1.0.0";
    public string Description => "ML-powered scenario generation";

    private ILogger<AdvancedScenarioGeneratorPlugin> _logger = null!;
    private IConfiguration _configuration = null!;

    public Task InitializeAsync(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<AdvancedScenarioGeneratorPlugin>>();
        _configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        _logger.LogInformation("Advanced Scenario Generator plugin initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _logger.LogInformation("Advanced Scenario Generator plugin shutting down");
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<GeneratedScenario>> GenerateScenariosAsync(
        ScenarioGenerationRequest request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating {Count} scenarios with ML algorithm", request.Count);

        var scenarios = new List<GeneratedScenario>();

        for (int i = 0; i < request.Count; i++)
        {
            var scenario = await GenerateMLScenario(request.RiskFocus);
            scenarios.Add(scenario);
        }

        return scenarios;
    }

    private async Task<GeneratedScenario> GenerateMLScenario(string riskFocus)
    {
        // Implement ML-based scenario generation
        await Task.Delay(100); // Simulate ML processing time

        return new GeneratedScenario
        {
            Name = $"ML Generated Scenario - {DateTime.UtcNow:HHmmss}",
            Description = "Generated using advanced ML algorithms",
            RiskLevel = riskFocus,
            Amount = Random.Shared.Next(1000, 1000000),
            GeneratedAt = DateTime.UtcNow
            // ... other properties
        };
    }
}
```

### 12.2 Custom Data Sources

#### Data Source Interface
```csharp
public interface IDataSource
{
    string Name { get; }
    string Description { get; }
    Task<bool> IsAvailableAsync();
    Task<IEnumerable<T>> GetDataAsync<T>(DataSourceQuery query) where T : class;
}

public class DataSourceQuery
{
    public string EntityType { get; set; } = string.Empty;
    public Dictionary<string, object> Filters { get; set; } = new();
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

// External API data source
public class ExternalApiDataSource : IDataSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiDataSource> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public string Name => "External API Data Source";
    public string Description => "Fetches data from external REST API";

    public ExternalApiDataSource(HttpClient httpClient, IConfiguration configuration, ILogger<ExternalApiDataSource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["DataSources:ExternalApi:BaseUrl"] ?? throw new ArgumentException("BaseUrl not configured");
        _apiKey = configuration["DataSources:ExternalApi:ApiKey"] ?? throw new ArgumentException("ApiKey not configured");
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External API health check failed");
            return false;
        }
    }

    public async Task<IEnumerable<T>> GetDataAsync<T>(DataSourceQuery query) where T : class
    {
        var url = $"{_baseUrl}/{query.EntityType}";
        var queryParams = new List<string>();

        // Add filters as query parameters
        foreach (var filter in query.Filters)
        {
            queryParams.Add($"{filter.Key}={Uri.EscapeDataString(filter.Value.ToString() ?? "")}");
        }

        if (query.Limit.HasValue)
            queryParams.Add($"limit={query.Limit}");
        if (query.Offset.HasValue)
            queryParams.Add($"offset={query.Offset}");

        if (queryParams.Any())
            url += "?" + string.Join("&", queryParams);

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<IEnumerable<T>>(json) ?? Enumerable.Empty<T>();

        return results;
    }
}
```

#### Data Source Manager
```csharp
public class DataSourceManager
{
    private readonly IEnumerable<IDataSource> _dataSources;
    private readonly ILogger<DataSourceManager> _logger;

    public DataSourceManager(IEnumerable<IDataSource> dataSources, ILogger<DataSourceManager> logger)
    {
        _dataSources = dataSources;
        _logger = logger;
    }

    public async Task<IDataSource?> GetAvailableDataSourceAsync(string name)
    {
        var dataSource = _dataSources.FirstOrDefault(ds => ds.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        
        if (dataSource == null)
        {
            _logger.LogWarning("Data source not found: {Name}", name);
            return null;
        }

        if (!await dataSource.IsAvailableAsync())
        {
            _logger.LogWarning("Data source not available: {Name}", name);
            return null;
        }

        return dataSource;
    }

    public async Task<IEnumerable<T>> GetDataFromFirstAvailableSourceAsync<T>(DataSourceQuery query) where T : class
    {
        foreach (var dataSource in _dataSources)
        {
            try
            {
                if (await dataSource.IsAvailableAsync())
                {
                    _logger.LogInformation("Using data source: {Name}", dataSource.Name);
                    return await dataSource.GetDataAsync<T>(query);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from source: {Name}", dataSource.Name);
            }
        }

        _logger.LogWarning("No available data sources for query: {EntityType}", query.EntityType);
        return Enumerable.Empty<T>();
    }
}
```

### 12.3 Custom Scenario Templates

#### Template Engine
```csharp
public interface ITemplateEngine
{
    Task<string> RenderAsync(string templateName, object model);
    Task<string> RenderFromStringAsync(string template, object model);
    void RegisterTemplate(string name, string template);
}

public class RazorTemplateEngine : ITemplateEngine
{
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, string> _templates = new();

    public async Task<string> RenderAsync(string templateName, object model)
    {
        if (!_templates.TryGetValue(templateName, out var template))
        {
            throw new ArgumentException($"Template not found: {templateName}");
        }

        return await RenderFromStringAsync(template, model);
    }

    public async Task<string> RenderFromStringAsync(string template, object model)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = _serviceProvider },
            new RouteData(),
            new ActionDescriptor());

        await using var stringWriter = new StringWriter();
        var viewResult = _razorViewEngine.GetView("", template, false);

        if (!viewResult.Success)
        {
            throw new InvalidOperationException($"Template compilation failed");
        }

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            new ViewDataDictionary<object>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            },
            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
            stringWriter,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return stringWriter.ToString();
    }

    public void RegisterTemplate(string name, string template)
    {
        _templates[name] = template;
    }
}
```

#### Custom Scenario Template
```csharp
public class ScenarioTemplateService
{
    private readonly ITemplateEngine _templateEngine;
    private readonly ILogger<ScenarioTemplateService> _logger;

    public ScenarioTemplateService(ITemplateEngine templateEngine, ILogger<ScenarioTemplateService> logger)
    {
        _templateEngine = templateEngine;
        _logger = logger;
        
        RegisterDefaultTemplates();
    }

    public async Task<string> GenerateScenarioAsync(string templateName, ScenarioModel model)
    {
        try
        {
            return await _templateEngine.RenderAsync(templateName, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate scenario from template: {TemplateName}", templateName);
            throw;
        }
    }

    private void RegisterDefaultTemplates()
    {
        // Register banking scenario template
        _templateEngine.RegisterTemplate("banking-fraud", @"
{
  ""model"": ""fraud-detector:stable"",
  ""messages"": [{
    ""role"": ""user"",
    ""content"": ""User Profile: {{Model.UserProfile}}
Activity: {{Model.Activity}}
Transaction Details:
- From: {{Model.FromName}} ({{Model.FromAccount}})
- To: {{Model.ToName}} ({{Model.ToAccount}})
- Amount: PKR {{Model.Amount:N0}}
- Date: {{Model.TransactionDate:dd/MM/yyyy HH:mm:ss}}
- Purpose: {{Model.Purpose}}

Risk Indicators:
- Amount Risk Score: {{Model.AmountRiskScore}}/10
- High Amount Flag: {{Model.HighAmountFlag ? \""Yes\"" : \""No\""}}
- Watchlist Match: {{Model.WatchlistMatch ? \""Yes\"" : \""No\""}}
- New Account: {{Model.NewAccount ? \""Yes\"" : \""No\""}}""
  }],
  ""stream"": false
}");

        // Register e-commerce scenario template
        _templateEngine.RegisterTemplate("ecommerce-fraud", @"
{
  ""model"": ""fraud-detector:stable"",
  ""messages"": [{
    ""role"": ""user"",
    ""content"": ""E-commerce Transaction Analysis:
Customer Profile: {{Model.UserProfile}}
Order Details:
- Order ID: {{Model.OrderId}}
- Items: {{Model.ItemCount}} items worth PKR {{Model.Amount:N0}}
- Payment Method: {{Model.PaymentMethod}}
- Shipping Address: {{Model.ShippingAddress}}
- IP Address: {{Model.IpAddress}}

Fraud Indicators:
- Velocity: {{Model.RecentOrders}} orders in last 24h
- Device Fingerprint: {{Model.DeviceFingerprint}}
- Location Match: {{Model.LocationMatch ? \""Yes\"" : \""No\""}}
- Card Country: {{Model.CardCountry}}""
  }],
  ""stream"": false
}");
    }
}

public class ScenarioModel
{
    public string UserProfile { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromAccount { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public int AmountRiskScore { get; set; }
    public bool HighAmountFlag { get; set; }
    public bool WatchlistMatch { get; set; }
    public bool NewAccount { get; set; }

    // E-commerce specific properties
    public string OrderId { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int RecentOrders { get; set; }
    public string DeviceFingerprint { get; set; } = string.Empty;
    public bool LocationMatch { get; set; }
    public string CardCountry { get; set; } = string.Empty;
}
```

### 12.4 Custom Reporting Engines

#### Report Engine Interface
```csharp
public interface IReportEngine
{
    string Name { get; }
    IEnumerable<string> SupportedFormats { get; }
    Task<ReportResult> GenerateReportAsync(ReportRequest request);
}

public class ReportRequest
{
    public string ReportType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ReportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

// Excel report engine implementation
public class ExcelReportEngine : IReportEngine
{
    public string Name => "Excel Report Engine";
    public IEnumerable<string> SupportedFormats => new[] { "xlsx", "xls" };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExcelReportEngine> _logger;

    public ExcelReportEngine(ApplicationDbContext context, ILogger<ExcelReportEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReportResult> GenerateReportAsync(ReportRequest request)
    {
        try
        {
            using var workbook = new XLWorkbook();
            
            switch (request.ReportType.ToLower())
            {
                case "scenarios":
                    await GenerateScenariosReport(workbook, request);
                    break;
                case "performance":
                    await GeneratePerformanceReport(workbook, request);
                    break;
                case "errors":
                    await GenerateErrorReport(workbook, request);
                    break;
                default:
                    throw new ArgumentException($"Unsupported report type: {request.ReportType}");
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            
            return new ReportResult
            {
                FileName = $"{request.ReportType}_{DateTime.Now:yyyyMMdd}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Data = stream.ToArray(),
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Excel report: {ReportType}", request.ReportType);
            
            return new ReportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task GenerateScenariosReport(XLWorkbook workbook, ReportRequest request)
    {
        var worksheet = workbook.Worksheets.Add("Scenarios");
        
        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Risk Level";
        worksheet.Cell(1, 4).Value = "Amount";
        worksheet.Cell(1, 5).Value = "Generated At";
        worksheet.Cell(1, 6).Value = "Tested";
        worksheet.Cell(1, 7).Value = "Test Result";

        // Data
        var query = _context.GeneratedScenarios.AsQueryable();
        
        if (request.StartDate.HasValue)
            query = query.Where(s => s.GeneratedAt >= request.StartDate.Value);
        if (request.EndDate.HasValue)
            query = query.Where(s => s.GeneratedAt <= request.EndDate.Value);

        var scenarios = await query.OrderByDescending(s => s.GeneratedAt).ToListAsync();
        
        int row = 2;
        foreach (var scenario in scenarios)
        {
            worksheet.Cell(row, 1).Value = scenario.Id;
            worksheet.Cell(row, 2).Value = scenario.Name;
            worksheet.Cell(row, 3).Value = scenario.RiskLevel;
            worksheet.Cell(row, 4).Value = scenario.Amount;
            worksheet.Cell(row, 5).Value = scenario.GeneratedAt;
            worksheet.Cell(row, 6).Value = scenario.IsTested ? "Yes" : "No";
            worksheet.Cell(row, 7).Value = scenario.TestSuccessful?.ToString() ?? "N/A";
            row++;
        }

        // Format headers
        var headerRange = worksheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
    }

    private async Task GeneratePerformanceReport(XLWorkbook workbook, ReportRequest request)
    {
        // Implementation for performance report
        var worksheet = workbook.Worksheets.Add("Performance");
        
        // Add performance metrics, charts, etc.
    }

    private async Task GenerateErrorReport(XLWorkbook workbook, ReportRequest request)
    {
        // Implementation for error analysis report
        var worksheet = workbook.Worksheets.Add("Errors");
        
        // Add error analysis data
    }
}
```

### 12.5 Event System

#### Event Publishing and Subscription
```csharp
public interface IApplicationEvent
{
    string EventType { get; }
    DateTime Timestamp { get; }
    Dictionary<string, object> Data { get; }
}

public class ScenarioGeneratedEvent : IApplicationEvent
{
    public string EventType => "scenario.generated";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();
    
    public int ScenarioId { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ApiTestCompletedEvent : IApplicationEvent
{
    public string EventType => "api.test.completed";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();
    
    public int ConfigurationId { get; set; }
    public int ScenarioId { get; set; }
    public bool Success { get; set; }
    public long ResponseTimeMs { get; set; }
}

public interface IEventHandler<TEvent> where TEvent : IApplicationEvent
{
    Task HandleAsync(TEvent eventData, CancellationToken cancellationToken = default);
}

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default) where TEvent : IApplicationEvent;
}

public class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default) where TEvent : IApplicationEvent
    {
        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();
        
        var tasks = handlers.Select(handler => 
            SafeHandleEventAsync(handler, eventData, cancellationToken));
        
        await Task.WhenAll(tasks);
    }

    private async Task SafeHandleEventAsync<TEvent>(
        IEventHandler<TEvent> handler, 
        TEvent eventData, 
        CancellationToken cancellationToken) where TEvent : IApplicationEvent
    {
        try
        {
            await handler.HandleAsync(eventData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType} with handler {HandlerType}", 
                eventData.EventType, handler.GetType().Name);
        }
    }
}

// Sample event handlers
public class ScenarioGeneratedNotificationHandler : IEventHandler<ScenarioGeneratedEvent>
{
    private readonly ILogger<ScenarioGeneratedNotificationHandler> _logger;
    private readonly IHubContext<ApiTestHub> _hubContext;

    public ScenarioGeneratedNotificationHandler(
        ILogger<ScenarioGeneratedNotificationHandler> logger,
        IHubContext<ApiTestHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task HandleAsync(ScenarioGeneratedEvent eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scenario generated: ID={ScenarioId}, Risk={RiskLevel}", 
            eventData.ScenarioId, eventData.RiskLevel);

        // Notify connected clients
        await _hubContext.Clients.All.SendAsync("ScenarioGenerated", new
        {
            eventData.ScenarioId,
            eventData.RiskLevel,
            eventData.Amount,
            eventData.Timestamp
        }, cancellationToken);
    }
}

public class PerformanceMetricsHandler : IEventHandler<ApiTestCompletedEvent>
{
    private readonly IMetrics _metrics;
    
    public PerformanceMetricsHandler(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("FraudDetector.Events");
        var testCounter = meter.CreateCounter<int>("api_tests_completed");
        var responseTimeHistogram = meter.CreateHistogram<double>("api_response_time");
    }

    public Task HandleAsync(ApiTestCompletedEvent eventData, CancellationToken cancellationToken = default)
    {
        // Record metrics
        _testCounter.Add(1, new TagList 
        { 
            { "configuration_id", eventData.ConfigurationId },
            { "success", eventData.Success } 
        });
        
        _responseTimeHistogram.Record(eventData.ResponseTimeMs, new TagList 
        { 
            { "configuration_id", eventData.ConfigurationId } 
        });

        return Task.CompletedTask;
    }
}
```

This completes the comprehensive Developer & Administrator Manual. The document covers all aspects of technical implementation, from system architecture and installation to advanced extension points and performance optimization strategies.

<function_calls>
<invoke name="mark_todo_as_done">
<parameter name="todo_ids">["cd06d37a-1b08-4628-94b4-ce56ce1dc1d2"]
