using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Services;
using FraudDetectorWebApp.Hubs;
using FraudDetectorWebApp.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configure enhanced logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
}
else
{
    // In production, be more restrictive with EF logging
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
}

// Add services to the container.
builder.Services.AddRazorPages();

// Add authentication services
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "FraudDetectorAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.MaxDepth = 64;
    });
builder.Services.AddSignalR();

// Add Entity Framework with enhanced configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });
    
    // Enhanced EF configuration
    if (builder.Environment.IsDevelopment())
    {
        // Enable detailed errors but NOT sensitive data logging for security
        options.EnableDetailedErrors();
        // Configure EF logging without sensitive data
        options.ConfigureWarnings(warnings => warnings.Log(RelationalEventId.CommandExecuted));
        // Note: Removed LogTo to prevent any sensitive data logging
    }
    
    options.EnableServiceProviderCaching();
});

// Add Repository Pattern
builder.Services.AddScoped<FraudDetectorWebApp.Repositories.IUserRepository, FraudDetectorWebApp.Repositories.UserRepository>();

// Add Database Seeder
builder.Services.AddScoped<DatabaseSeeder>();

// Add HttpClient for API requests
builder.Services.AddHttpClient();

// Add background service as singleton
builder.Services.AddSingleton<ApiRequestService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ApiRequestService>());

// Add CORS for API calls from frontend and SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins("http://localhost", "https://localhost")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure database migrations are applied
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Use migrations instead of EnsureCreated for better database management
        context.Database.Migrate();
        
        // Seed default data if needed
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
        throw;
    }
}


// Add custom error logging middleware
app.UseMiddleware<ErrorLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map API controllers
app.MapControllers();
app.MapRazorPages();
app.MapHub<ApiTestHub>("/hubs/apitest");

app.Run();
