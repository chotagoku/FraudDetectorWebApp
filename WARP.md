# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

This is a fraud detection web application built with ASP.NET Core 9.0. The application serves as an automated API testing platform for fraud detection systems, allowing users to configure API endpoints, generate test scenarios, and monitor API performance with real-time feedback.

## Technology Stack

- **Framework**: ASP.NET Core 9.0 with Razor Pages
- **Database**: SQL Server with Entity Framework Core
- **Real-time Communication**: SignalR for live updates
- **Frontend**: Server-side rendered Razor Pages with JavaScript
- **Background Services**: Hosted services for automated API testing

## Common Development Commands

### Building and Running
```powershell
# Build the project
dotnet build

# Run in development mode (includes hot reload)
dotnet run

# Run in specific environment
dotnet run --environment Development
dotnet run --environment Production

# Publish for deployment
dotnet publish -c Release -o ./publish
```

### Database Operations
```powershell
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Drop database (destructive)
dotnet ef database drop

# Generate SQL script from migrations
dotnet ef migrations script

# Rollback to specific migration
dotnet ef database update PreviousMigrationName
```

### Testing and Code Quality
```powershell
# Run all tests (if tests exist)
dotnet test

# Build with specific configuration
dotnet build -c Debug
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

## Application Architecture

### Core Components

**Program.cs** - Application entry point and service configuration
- Configures Entity Framework with SQL Server
- Sets up SignalR for real-time communication
- Registers background services and HTTP clients
- Configures CORS policies for API access

**ApiRequestService** - Background service managing automated API testing
- Runs continuously checking for active API configurations
- Generates dynamic test payloads with randomized data
- Handles SSL certificate validation bypass
- Provides real-time notifications via SignalR
- Supports start/stop operations and iteration limits

### Data Models

**ApiConfiguration** - Stores API endpoint configurations
- Contains endpoint URL, request template, authentication
- Supports Bearer token authentication
- Configurable delays and iteration limits
- Soft deletion with IsDeleted flag

**ApiRequestLog** - Tracks all API test executions
- Stores request/response data and performance metrics
- Links to parent ApiConfiguration
- Captures success/failure status and error messages

**GeneratedScenario** - Stores generated fraud test scenarios
- Contains comprehensive fraud detection test data
- Includes risk scoring, watchlist indicators, and user profiles
- Supports testing against multiple API configurations

### Controllers Architecture

**ConfigurationController** - Manages API test configurations
- CRUD operations for API configurations
- Start/stop individual or all configurations
- Provides system status information

**GenerationsController** - Handles scenario generation and testing
- Generates fraud detection test scenarios
- Supports batch scenario creation
- Tests individual scenarios against APIs

**ResultsController** - Provides test results and analytics
- Paginated access to test results
- Configuration-specific result filtering
- Performance statistics and reporting

### Database Design

The application uses Entity Framework with a code-first approach:
- **Soft deletion** implemented across all entities
- **Query filters** automatically exclude deleted records
- **Comprehensive indexing** for performance optimization
- **Foreign key relationships** with appropriate cascade behaviors

### Real-time Features

**SignalR Hub (ApiTestHub)** - Enables real-time dashboard updates
- Automatic client grouping for dashboard connections
- Live notifications for new test results
- System status change broadcasts

## Configuration

### Database Connection
Configure in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Initial Catalog=FraudDetectorApp;User ID=YourUsername;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

### Application Settings
- Logging levels configurable per namespace
- Entity Framework logging can be adjusted for debugging
- CORS policies configured for both API and SignalR access

## Key Features

### Dynamic Payload Generation
The ApiRequestService generates realistic test data including:
- Random financial transaction amounts and account numbers
- Varied user profiles and activity patterns  
- Pakistani banking system specific data (IBAN, bank codes)
- Risk scoring and watchlist indicators
- Timestamps and transaction metadata

### Background Processing
- Continuous monitoring of active API configurations
- Automatic deactivation when iteration limits reached
- Error handling and retry logic
- Performance monitoring and logging

### API Template System
Supports placeholder replacement in request templates:
- `{{iteration}}` - Current iteration number
- `{{timestamp}}` - Current timestamp
- `{{random_amount}}` - Random transaction amount
- `{{user_profile}}` - Random user profile description
- Plus many more fraud-detection specific placeholders

## Development Notes

### Entity Framework Patterns
- All entities implement soft deletion via `ISoftDelete` interface
- Query filters automatically applied in `ApplicationDbContext`
- Comprehensive indexing strategy for performance
- Navigation properties properly configured

### SignalR Integration
- Automatic client connection management
- Group-based messaging for dashboard updates
- Error handling in SignalR notifications
- Connection lifecycle management in hub

### Background Service Patterns
- Proper dependency injection scoping in background services
- Cancellation token support for graceful shutdowns
- Exception handling with configurable retry delays
- Integration with Entity Framework contexts

### Security Considerations
- Bearer token authentication support
- SSL certificate validation bypass option for development
- SQL injection prevention through parameterized queries
- CORS policies configured appropriately

## Common Development Scenarios

### Adding New API Placeholders
1. Add placeholder logic to `PrepareRequestPayload` method in `ApiRequestService`
2. Update documentation for available placeholders
3. Test with various API configurations

### Extending Data Models
1. Update model classes in `Models` folder
2. Configure relationships in `ApplicationDbContext.OnModelCreating`
3. Create and apply Entity Framework migration
4. Update controllers and views as needed

### Adding New API Endpoints
1. Create or extend controller in `Controllers` folder
2. Implement proper error handling and validation
3. Update any related SignalR notifications
4. Add appropriate logging

### Modifying Background Processing
1. Update `ApiRequestService` for new processing logic
2. Consider impact on existing configurations
3. Test start/stop functionality
4. Verify SignalR notifications work correctly

## Database Schema

The application uses three main tables:
- **ApiConfigurations** - API endpoint configurations with request templates
- **ApiRequestLogs** - Detailed logs of all API test executions  
- **GeneratedScenarios** - Generated fraud detection test scenarios

All tables implement soft deletion and have comprehensive indexing for performance.
