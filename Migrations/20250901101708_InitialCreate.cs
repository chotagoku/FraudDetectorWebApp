using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraudDetectorWebApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    RequiresRestart = table.Column<bool>(type: "bit", nullable: false),
                    IsAdvanced = table.Column<bool>(type: "bit", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowedValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Section = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "User"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BearerToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DelayBetweenRequests = table.Column<int>(type: "int", nullable: false),
                    MaxIterations = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TrustSslCertificate = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiConfigurations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BetaScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserStory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedStory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionStory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScenarioJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserProfile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BusinessType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerSegment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ToAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ActivityCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AmountRiskScore = table.Column<int>(type: "int", nullable: false),
                    AmountZScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FraudScore = table.Column<int>(type: "int", nullable: false),
                    ComplianceScore = table.Column<int>(type: "int", nullable: false),
                    HighAmountFlag = table.Column<bool>(type: "bit", nullable: false),
                    SuspiciousActivityFlag = table.Column<bool>(type: "bit", nullable: false),
                    ComplianceFlag = table.Column<bool>(type: "bit", nullable: false),
                    AMLFlag = table.Column<bool>(type: "bit", nullable: false),
                    CTFFlag = table.Column<bool>(type: "bit", nullable: false),
                    NewActivityCode = table.Column<bool>(type: "bit", nullable: false),
                    NewFromAccount = table.Column<bool>(type: "bit", nullable: false),
                    NewToAccount = table.Column<bool>(type: "bit", nullable: false),
                    NewToCity = table.Column<bool>(type: "bit", nullable: false),
                    OutsideUsualDay = table.Column<bool>(type: "bit", nullable: false),
                    OfficeHours = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistFromAccount = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistFromName = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistToAccount = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistToName = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistToBank = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistIPAddress = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistCNIC = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistPhoneNumber = table.Column<bool>(type: "bit", nullable: false),
                    CNIC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransactionDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ToBank = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GenerationPrompt = table.Column<string>(type: "ntext", nullable: true),
                    GenerationEngine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    GeneratedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsTested = table.Column<bool>(type: "bit", nullable: false),
                    TestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TestResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
                    TestSuccessful = table.Column<bool>(type: "bit", nullable: true),
                    TestErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestCount = table.Column<int>(type: "int", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastStatusCode = table.Column<int>(type: "int", nullable: true),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsedDatabaseData = table.Column<bool>(type: "bit", nullable: false),
                    SourceDataSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConfigurationId = table.Column<int>(type: "int", nullable: true),
                    BasedOnScenarioId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BetaScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BetaScenarios_ApiConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "ApiConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BetaScenarios_BetaScenarios_BasedOnScenarioId",
                        column: x => x.BasedOnScenarioId,
                        principalTable: "BetaScenarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GeneratedScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScenarioJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UserProfile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserActivity = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AmountRiskScore = table.Column<int>(type: "int", nullable: false),
                    AmountZScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    HighAmountFlag = table.Column<bool>(type: "bit", nullable: false),
                    HasWatchlistMatch = table.Column<bool>(type: "bit", nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActivityCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsTested = table.Column<bool>(type: "bit", nullable: false),
                    TestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TestResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
                    TestSuccessful = table.Column<bool>(type: "bit", nullable: true),
                    TestErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedPrompt = table.Column<string>(type: "ntext", nullable: true),
                    FromAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CNIC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransactionDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TransactionComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ToBank = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NewActivityCode = table.Column<bool>(type: "bit", nullable: false),
                    NewFromAccount = table.Column<bool>(type: "bit", nullable: false),
                    NewToAccount = table.Column<bool>(type: "bit", nullable: false),
                    NewToCity = table.Column<bool>(type: "bit", nullable: false),
                    OutsideUsualDay = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistFromAccount = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistFromName = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistToAccount = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistToName = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistToBank = table.Column<bool>(type: "bit", nullable: false),
                    WatchlistIPAddress = table.Column<bool>(type: "bit", nullable: false),
                    TestCount = table.Column<int>(type: "int", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTestConfiguration = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastStatusCode = table.Column<int>(type: "int", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConfigurationId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedScenarios_ApiConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "ApiConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ApiRequestLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiConfigurationId = table.Column<int>(type: "int", nullable: false),
                    RequestPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    IterationNumber = table.Column<int>(type: "int", nullable: false),
                    GeneratedScenarioId = table.Column<int>(type: "int", nullable: true),
                    BetaScenarioId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiRequestLogs_ApiConfigurations_ApiConfigurationId",
                        column: x => x.ApiConfigurationId,
                        principalTable: "ApiConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiRequestLogs_BetaScenarios_BetaScenarioId",
                        column: x => x.BetaScenarioId,
                        principalTable: "BetaScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ApiRequestLogs_GeneratedScenarios_GeneratedScenarioId",
                        column: x => x.GeneratedScenarioId,
                        principalTable: "GeneratedScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_Name",
                table: "ApiConfigurations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_UserId",
                table: "ApiConfigurations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_ApiConfigurationId_IterationNumber",
                table: "ApiRequestLogs",
                columns: new[] { "ApiConfigurationId", "IterationNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_BetaScenarioId",
                table: "ApiRequestLogs",
                column: "BetaScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_GeneratedScenarioId",
                table: "ApiRequestLogs",
                column: "GeneratedScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiRequestLogs_RequestTimestamp",
                table: "ApiRequestLogs",
                column: "RequestTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_Amount",
                table: "BetaScenarios",
                column: "Amount");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_BasedOnScenarioId",
                table: "BetaScenarios",
                column: "BasedOnScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_Category",
                table: "BetaScenarios",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_Category_Status",
                table: "BetaScenarios",
                columns: new[] { "Category", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_ComplianceScore",
                table: "BetaScenarios",
                column: "ComplianceScore");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_ConfigurationId",
                table: "BetaScenarios",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_ConfigurationId_GeneratedAt",
                table: "BetaScenarios",
                columns: new[] { "ConfigurationId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_FraudScore",
                table: "BetaScenarios",
                column: "FraudScore");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_GeneratedAt",
                table: "BetaScenarios",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_GeneratedBy",
                table: "BetaScenarios",
                column: "GeneratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_GeneratedBy_GeneratedAt",
                table: "BetaScenarios",
                columns: new[] { "GeneratedBy", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_IsFavorite",
                table: "BetaScenarios",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_IsTested",
                table: "BetaScenarios",
                column: "IsTested");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_Priority",
                table: "BetaScenarios",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_RiskLevel",
                table: "BetaScenarios",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_RiskLevel_IsTested",
                table: "BetaScenarios",
                columns: new[] { "RiskLevel", "IsTested" });

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_Status",
                table: "BetaScenarios",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BetaScenarios_TestSuccessful",
                table: "BetaScenarios",
                column: "TestSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_Amount",
                table: "GeneratedScenarios",
                column: "Amount");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_AmountRiskScore",
                table: "GeneratedScenarios",
                column: "AmountRiskScore");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_AmountRiskScore_Amount",
                table: "GeneratedScenarios",
                columns: new[] { "AmountRiskScore", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_ConfigurationId",
                table: "GeneratedScenarios",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_ConfigurationId_GeneratedAt",
                table: "GeneratedScenarios",
                columns: new[] { "ConfigurationId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_GeneratedAt",
                table: "GeneratedScenarios",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_IsFavorite",
                table: "GeneratedScenarios",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_IsTested",
                table: "GeneratedScenarios",
                column: "IsTested");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_RiskLevel",
                table: "GeneratedScenarios",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_RiskLevel_IsTested",
                table: "GeneratedScenarios",
                columns: new[] { "RiskLevel", "IsTested" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedScenarios_TestSuccessful",
                table: "GeneratedScenarios",
                column: "TestSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Category",
                table: "SystemConfigurations",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Category_Section",
                table: "SystemConfigurations",
                columns: new[] { "Category", "Section" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_IsAdvanced",
                table: "SystemConfigurations",
                column: "IsAdvanced");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_IsReadOnly",
                table: "SystemConfigurations",
                column: "IsReadOnly");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_IsReadOnly_RequiresRestart",
                table: "SystemConfigurations",
                columns: new[] { "IsReadOnly", "RequiresRestart" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Key",
                table: "SystemConfigurations",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_RequiresRestart",
                table: "SystemConfigurations",
                column: "RequiresRestart");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Section",
                table: "SystemConfigurations",
                column: "Section");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_UpdatedAt",
                table: "SystemConfigurations",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiRequestLogs");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");

            migrationBuilder.DropTable(
                name: "BetaScenarios");

            migrationBuilder.DropTable(
                name: "GeneratedScenarios");

            migrationBuilder.DropTable(
                name: "ApiConfigurations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
