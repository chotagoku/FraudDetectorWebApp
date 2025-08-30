# FraudDetectorWebApp - User Manual

**Version:** 1.0  
**Date:** 29 August 2025  
**Application Version:** .NET 9.0

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Getting Started](#2-getting-started)
3. [Dashboard Overview](#3-dashboard-overview)
4. [Scenario Generator](#4-scenario-generator)
5. [Configuration Management](#5-configuration-management)
6. [Testing & Results](#6-testing--results)
7. [Reports & Analytics](#7-reports--analytics)
8. [Troubleshooting](#8-troubleshooting)
9. [Frequently Asked Questions](#9-frequently-asked-questions)
10. [Glossary](#10-glossary)

---

## 1. Introduction

### What is FraudDetectorWebApp?

FraudDetectorWebApp is a comprehensive fraud detection testing platform that helps organizations:
- **Generate realistic fraud scenarios** for testing fraud detection systems
- **Test API endpoints** with automated scenario data
- **Monitor performance** of fraud detection APIs in real-time
- **Analyze results** with detailed reporting and analytics

### Who Should Use This Manual?

This manual is designed for:
- **Business Users** who need to generate and test fraud scenarios
- **QA Testers** validating fraud detection systems
- **Compliance Officers** requiring audit trails and reporting
- **System Administrators** managing API configurations

### Key Benefits

✅ **Realistic Test Data**: Generate thousands of realistic fraud scenarios  
✅ **Automated Testing**: Continuous API testing with background monitoring  
✅ **Real-time Analytics**: Live dashboards and performance metrics  
✅ **Comprehensive Reporting**: Detailed analytics with export capabilities  
✅ **Easy Configuration**: User-friendly interface for API management  

---

## 2. Getting Started

### 2.1 System Requirements

#### Minimum Requirements
- **Operating System**: Windows 10+ or modern web browser
- **Browser**: Chrome 90+, Firefox 88+, Edge 90+, Safari 14+
- **Internet Connection**: Required for API testing
- **Screen Resolution**: 1024x768 minimum (1920x1080 recommended)

#### Recommended Specifications
- **Browser**: Latest version of Chrome or Edge
- **Screen Resolution**: 1920x1080 or higher
- **Memory**: 4GB RAM available

### 2.2 Accessing the Application

1. **Open your web browser**
2. **Navigate to the application URL** (provided by your administrator)
3. **The application loads automatically** - no login required initially

> **Note**: If you encounter access issues, contact your system administrator.

### 2.3 First-Time Setup

When you first access the application:

1. **Review the Dashboard** to understand the interface
2. **Check System Status** in the top section
3. **Explore Available Features** through the navigation cards
4. **Create Your First Configuration** (see Section 5)

---

## 3. Dashboard Overview

### 3.1 Main Dashboard Components

The dashboard is your central hub for all fraud testing activities:

#### Navigation Cards
| Card | Purpose | Action |
|------|---------|---------|
| **Scenario Generator** | Create fraud test scenarios | Click to access generator |
| **Test Results** | View API testing results | Click to see results |
| **Reports** | Access analytics and reports | Click for reporting |
| **Configuration** | Manage API settings | Click to configure |

#### System Status Section
- **System Control**: Start/stop automated testing
- **Real-time Status**: Shows if testing is active
- **Quick Actions**: Refresh, clear logs, add configurations

#### Statistics Cards
- **Total Requests**: Number of API calls made
- **Successful**: Number of successful API responses
- **Failed**: Number of failed API calls
- **Avg Response Time**: Average API response time

#### Live Results Panel
- **Real-time Updates**: Shows live API test results
- **Performance Metrics**: Response times and success rates
- **Configuration Status**: Active/inactive configurations

### 3.2 System Status Indicators

| Status | Indicator | Meaning |
|---------|-----------|---------|
| **Active** | 🟢 Green | System is running and testing APIs |
| **Stopped** | 🔴 Red | System is stopped, no testing |
| **Loading** | 🟡 Yellow | System is starting/stopping |

---

## 4. Scenario Generator

### 4.1 Overview

The Scenario Generator creates realistic fraud detection test cases with configurable parameters.

### 4.2 Accessing the Generator

1. **From Dashboard**: Click the "Scenario Generator" card
2. **Direct Navigation**: Use the top menu "Generator" option

### 4.3 Generation Settings

#### Number of Scenarios
Choose how many scenarios to generate:
- **1 Scenario**: Single test case
- **3 Scenarios**: Small batch (default)
- **5 Scenarios**: Medium batch
- **10 Scenarios**: Large batch
- **25-50 Scenarios**: Bulk generation

#### Risk Level Focus
Control the risk distribution:
- **Mixed (Random)**: Balanced distribution (recommended)
- **Low Risk Only**: Risk scores 1-3
- **Medium Risk Only**: Risk scores 4-6  
- **High Risk Only**: Risk scores 7-10

#### Output Format
Choose how to display results:
- **JSON Format**: Technical JSON structure
- **Table View**: User-friendly table display

#### Save to Database
- **Enabled**: Scenarios saved for future use
- **Disabled**: Generate temporary scenarios only

### 4.4 Generation Process

1. **Configure Settings**: Select your preferred options
2. **Click "Generate Random Scenarios"**: Creates scenarios based on settings
3. **Review Generated Data**: Examine scenarios in chosen format
4. **Copy or Download**: Save scenarios for external use

### 4.5 Advanced Features

#### Load Saved Scenarios
- **Purpose**: Reuse previously generated scenarios
- **Action**: Click "Load Saved" button
- **Result**: Displays historical scenarios from database

#### Load Favorites
- **Purpose**: Access your favorite scenarios
- **Action**: Click "Favorites" button
- **Result**: Shows scenarios marked as favorites

#### Random from Database
- **Purpose**: Get random scenarios from existing data
- **Action**: Click "Random DB" button
- **Result**: Loads random scenarios matching current filters

### 4.6 Understanding Generated Scenarios

Each scenario contains comprehensive fraud indicators:

#### User Profile Information
- **Customer Type**: Business, individual, corporate account
- **Activity Pattern**: Transaction frequency and behavior
- **Historical Context**: Past transaction patterns

#### Transaction Details
- **Financial Information**: Amount, accounts, bank details
- **Timing**: Date, time, and frequency patterns
- **Context**: Transaction purpose and comments

#### Risk Indicators
- **Amount Risk Score**: 1-10 scale (higher = riskier)
- **Amount Z-Score**: Statistical deviation from normal
- **Behavioral Flags**: New accounts, unusual timing
- **Watchlist Matches**: Known fraudulent entities

#### Example Scenario Structure
```json
{
  "model": "fraud-detector:stable",
  "messages": [{
    "role": "user",
    "content": "User Profile: Customer is a small business owner..."
  }],
  "stream": false
}
```

---

## 5. Configuration Management

### 5.1 Purpose of Configurations

Configurations define:
- **API Endpoints**: Where to send test requests
- **Authentication**: How to authenticate with APIs
- **Request Templates**: What data to send
- **Testing Parameters**: Frequency, limits, and timing

### 5.2 Creating a New Configuration

#### From Dashboard
1. **Click "New Configuration"** button
2. **Configuration modal opens**

#### From Direct Access
1. **Click the Configuration card**
2. **Click "Add Configuration"**

#### Required Information

**Basic Settings:**
- **Configuration Name**: Descriptive name (e.g., "Production Fraud API")
- **API Endpoint**: Full URL (e.g., "https://api.example.com/fraud-detect")

**Request Template:**
```json
{
  "model": "fraud-detector:stable",
  "messages": [
    {
      "role": "user",
      "content": "{{user_profile}} Transaction: {{from_name}} to {{to_name}} Amount: {{random_amount}}"
    }
  ],
  "stream": false
}
```

**Authentication (Optional):**
- **Bearer Token**: API authentication token
- **Stored securely** in the application

**Testing Parameters:**
- **Delay Between Requests**: Milliseconds (default: 5000)
- **Max Iterations**: Maximum tests (0 = unlimited)

#### Template Placeholders

The system supports dynamic placeholders:

| Placeholder | Example | Description |
|-------------|---------|-------------|
| `{{iteration}}` | 1, 2, 3... | Current iteration number |
| `{{timestamp}}` | 8/29/2025 2:30 PM | Current date/time |
| `{{random_amount}}` | 150000 | Random transaction amount |
| `{{user_profile}}` | Small business owner | Random user profile |
| `{{from_name}}` | AHMED TRADERS | Random sender name |
| `{{to_name}}` | K-ELECTRIC | Random recipient name |
| `{{activity_code}}` | Bill Payment | Random activity type |

### 5.3 Managing Configurations

#### Viewing Configurations
- **Dashboard**: Lists all configurations with status
- **Details**: Shows endpoints, tokens, and performance

#### Editing Configurations
1. **Click configuration name** or edit button
2. **Modify settings** as needed
3. **Save changes**

#### Deleting Configurations
1. **Click delete button** (trash icon)
2. **Confirm deletion**
3. **Configuration removed permanently**

#### Starting/Stopping Testing
- **Start Individual**: Click play button for specific configuration
- **Stop Individual**: Click stop button for specific configuration
- **Start All**: Use "Start All" button in system controls
- **Stop All**: Use "Stop All" button in system controls

### 5.4 Configuration Status

| Status | Indicator | Meaning |
|---------|-----------|---------|
| **Active** | ▶️ Running | Configuration is actively testing |
| **Inactive** | ⏹️ Stopped | Configuration is not testing |
| **Completed** | ✅ Done | Reached maximum iterations |
| **Error** | ❌ Failed | Configuration encountered error |

---

## 6. Testing & Results

### 6.1 Automated Testing

#### How It Works
1. **Background Service**: Continuously runs tests
2. **Uses Configurations**: Tests active API configurations
3. **Real-time Updates**: Shows results as they happen
4. **Detailed Logging**: Records all requests and responses

#### Test Execution
- **Sequential Processing**: Tests configurations one by one
- **Configurable Delays**: Respects delay settings
- **Error Handling**: Continues testing despite individual failures
- **Automatic Stopping**: Stops when max iterations reached

### 6.2 Viewing Results

#### From Dashboard
- **Live Results Panel**: Shows recent test results
- **Click for Details**: View full result information

#### Results Page
1. **Navigation**: Click "Test Results" card or menu
2. **Filtering**: Choose configuration and date ranges
3. **View Options**: Standard or story view

#### Result Information
Each result shows:
- **Iteration Number**: Test sequence number
- **Timestamp**: When test was executed
- **Success/Failure**: Test outcome
- **Response Time**: API response duration
- **Status Code**: HTTP response code
- **Request/Response Data**: Full technical details

### 6.3 Understanding Results

#### Success Indicators
- **Green Icons**: ✅ Successful API response
- **200 Status Codes**: HTTP success
- **Response Time**: Measured in milliseconds
- **Response Content**: API returned data

#### Failure Indicators
- **Red Icons**: ❌ Failed API response
- **Error Status Codes**: 400, 500, etc.
- **Error Messages**: Detailed failure reasons
- **Timeout Issues**: Network or performance problems

#### Performance Metrics
- **Response Time**: How fast the API responds
- **Success Rate**: Percentage of successful tests
- **Average Performance**: Mean response time
- **Fastest Response**: Best performance recorded

### 6.4 Filtering and Searching

#### Available Filters
- **Configuration**: Test specific API endpoints
- **Status**: Success/failure filter
- **Date Range**: Time-based filtering
- **Results per Page**: Control display quantity

#### Search Capabilities
- **Real-time Filtering**: Results update as you filter
- **Multiple Criteria**: Combine filters for precise results
- **Saved Preferences**: Settings remembered per session

### 6.5 Export and Sharing

#### Export Options
- **CSV Format**: Spreadsheet-compatible data
- **JSON Format**: Technical data structure
- **Copy to Clipboard**: Quick data sharing

#### Report Generation
- **Summary Statistics**: Overall performance metrics
- **Detailed Results**: Individual test outcomes
- **Performance Analysis**: Trends and patterns

---

## 7. Reports & Analytics

### 7.1 Reports Overview

The Reports section provides comprehensive analytics for:
- **Generated Scenarios**: Analysis of created test data
- **API Performance**: Testing results and metrics
- **Configuration Analysis**: Endpoint performance comparison
- **Trend Analysis**: Performance over time

### 7.2 Accessing Reports

1. **From Dashboard**: Click "Reports" card
2. **Navigation Menu**: Select "Reports" option
3. **Direct URL**: Navigate to reports page

### 7.3 Report Types

#### Generated Scenarios Report
- **Total Scenarios**: Count of created scenarios
- **Risk Distribution**: Breakdown by risk levels
- **Generation Trends**: Creation patterns over time
- **Popular Templates**: Most used scenario types

#### API Test Results Report
- **Performance Metrics**: Response times and success rates
- **Error Analysis**: Failure patterns and causes
- **Volume Statistics**: Request counts and frequency
- **Reliability Trends**: Performance consistency

#### Configuration Analytics
- **Endpoint Comparison**: Performance across different APIs
- **Usage Statistics**: Most/least tested configurations
- **Resource Utilization**: System load and efficiency
- **Cost Analysis**: Testing resource consumption

#### Comparative Analysis
- **Before/After**: Performance comparisons
- **A/B Testing**: Different configuration variants
- **Benchmark Analysis**: Against performance targets
- **Trend Identification**: Performance patterns

### 7.4 Report Filters

#### Filter Options
- **Report Type**: Select analysis focus
- **Configuration**: Specific API endpoints
- **Risk Level**: Scenario risk categorization
- **Date Range**: Time period selection
- **Custom Ranges**: Specific date/time periods

#### Advanced Filtering
- **Multiple Criteria**: Combine filters
- **Save Filter Sets**: Reuse common filter combinations
- **Quick Presets**: Today, week, month, quarter

### 7.5 Data Visualization

#### Summary Cards
- **Key Metrics**: Important statistics at a glance
- **Color Coding**: Visual status indicators
- **Trend Arrows**: Performance direction
- **Comparison Values**: Period-over-period changes

#### Data Tables
- **Sortable Columns**: Click headers to sort
- **Pagination**: Navigate large datasets
- **Row Details**: Click for expanded information
- **Bulk Actions**: Select multiple rows

#### Charts and Graphs
- **Performance Trends**: Line charts showing changes over time
- **Distribution Analysis**: Pie charts for categorical data
- **Comparison Charts**: Bar charts comparing configurations
- **Real-time Updates**: Live data refresh

### 7.6 Exporting Reports

#### Export Formats
- **Excel (.xlsx)**: Full spreadsheet with formatting
- **CSV**: Comma-separated values for data analysis
- **JSON**: Technical data format
- **PDF**: Formatted report for sharing

#### Export Options
- **Current View**: Export what's currently displayed
- **Full Dataset**: Export all matching data
- **Summary Only**: Key metrics and totals
- **Custom Selection**: Choose specific columns/rows

---

## 8. Troubleshooting

### 8.1 Common Issues

#### Application Won't Load
**Symptoms**: Blank page or loading spinner
**Solutions**:
1. **Check Internet Connection**: Verify network connectivity
2. **Clear Browser Cache**: Ctrl+F5 to refresh
3. **Try Different Browser**: Chrome, Firefox, or Edge
4. **Disable Browser Extensions**: May interfere with application
5. **Contact Administrator**: If issues persist

#### Configuration Errors
**Symptoms**: "Configuration failed" messages
**Solutions**:
1. **Check API Endpoint**: Verify URL is correct and accessible
2. **Validate Bearer Token**: Ensure token is valid and not expired
3. **Test Template Syntax**: Verify JSON template is valid
4. **Check Network Access**: Ensure API endpoint is reachable
5. **Review Error Messages**: Look for specific error details

#### Test Results Not Appearing
**Symptoms**: Tests run but no results show
**Solutions**:
1. **Refresh Page**: F5 or browser refresh
2. **Check System Status**: Ensure testing is active
3. **Verify Configuration**: Confirm configuration is active
4. **Check Filters**: Ensure results aren't filtered out
5. **Wait for Processing**: Large batches may take time

#### Slow Performance
**Symptoms**: Application responds slowly
**Solutions**:
1. **Reduce Batch Size**: Generate fewer scenarios at once
2. **Increase Delays**: Add more delay between API requests
3. **Close Other Tabs**: Free up browser memory
4. **Clear Browser Data**: Remove cached data
5. **Check Network Speed**: Ensure adequate bandwidth

#### API Testing Failures
**Symptoms**: High failure rates in testing
**Solutions**:
1. **Check API Status**: Verify endpoint is operational
2. **Review Authentication**: Ensure bearer token is correct
3. **Validate Request Format**: Check template syntax
4. **Increase Delays**: Reduce request frequency
5. **Contact API Provider**: Report persistent issues

### 8.2 Error Messages

#### "Invalid Configuration"
- **Cause**: Configuration settings are incorrect
- **Solution**: Review and correct configuration parameters
- **Details**: Check endpoint URL, authentication, and template

#### "API Request Failed"
- **Cause**: API endpoint returned error
- **Solution**: Check API status and request format
- **Details**: Review error response for specific issue

#### "Authentication Failed"
- **Cause**: Bearer token is invalid or expired
- **Solution**: Update bearer token in configuration
- **Details**: Contact API provider for new token

#### "Network Error"
- **Cause**: Cannot reach API endpoint
- **Solution**: Check network connectivity and endpoint URL
- **Details**: Verify firewall settings and DNS resolution

#### "Rate Limit Exceeded"
- **Cause**: Too many requests to API
- **Solution**: Increase delay between requests
- **Details**: Reduce testing frequency or batch size

### 8.3 Performance Optimization

#### Browser Performance
1. **Use Latest Browser**: Keep browser updated
2. **Close Unused Tabs**: Reduce memory usage
3. **Clear Cache Regularly**: Prevent data buildup
4. **Disable Unnecessary Extensions**: Reduce conflicts

#### Application Performance
1. **Reasonable Batch Sizes**: Don't generate too many scenarios at once
2. **Appropriate Delays**: Don't overwhelm APIs with requests
3. **Regular Cleanup**: Clear old test results periodically
4. **Monitor Resources**: Watch system resource usage

### 8.4 Getting Help

#### Self-Service Resources
- **This User Manual**: Comprehensive usage information
- **FAQ Section**: Common questions and answers
- **Error Messages**: Built-in help for error conditions

#### Contacting Support
- **System Administrator**: For access and configuration issues
- **Technical Support**: For application bugs or problems
- **API Provider**: For endpoint-specific issues
- **Include Details**: Error messages, steps taken, screenshots

---

## 9. Frequently Asked Questions

### 9.1 General Questions

**Q: What is the purpose of FraudDetectorWebApp?**
A: It's a testing platform that generates realistic fraud scenarios and tests fraud detection APIs to ensure they work correctly.

**Q: Do I need special training to use this application?**
A: No, the application is designed to be user-friendly. This manual provides all necessary information.

**Q: Can multiple people use the application simultaneously?**
A: Yes, the application supports multiple concurrent users.

**Q: Is my data secure?**
A: The application uses industry-standard security practices. However, avoid using real customer data in testing.

### 9.2 Scenario Generation

**Q: How realistic are the generated scenarios?**
A: Very realistic. The system uses sophisticated algorithms to create scenarios that mirror real-world fraud patterns.

**Q: Can I customize the types of scenarios generated?**
A: Yes, you can control risk levels, user profiles, and many other parameters.

**Q: How many scenarios can I generate at once?**
A: You can generate from 1 to 50 scenarios in a single batch, depending on your needs.

**Q: Are generated scenarios saved permanently?**
A: Yes, if you enable "Save to Database". Otherwise, they're temporary.

### 9.3 API Testing

**Q: What types of APIs can I test?**
A: Any REST API that accepts JSON requests and returns responses.

**Q: How often does the system test APIs?**
A: You configure the frequency. Default is every 5 seconds, but this is adjustable.

**Q: Can I test multiple APIs simultaneously?**
A: Yes, you can have multiple configurations running at the same time.

**Q: What happens if an API is down?**
A: The system logs the failure and continues testing. It doesn't stop the entire process.

### 9.4 Results and Reporting

**Q: How long are test results kept?**
A: Results are kept permanently unless manually deleted or cleared.

**Q: Can I export results for external analysis?**
A: Yes, you can export in CSV, JSON, and Excel formats.

**Q: What metrics should I focus on?**
A: Success rate, response time, and error patterns are key performance indicators.

**Q: Can I create scheduled reports?**
A: Currently, reports are generated on-demand. Scheduling is a planned future feature.

### 9.5 Technical Questions

**Q: What browsers are supported?**
A: Chrome, Firefox, Edge, and Safari (latest versions recommended).

**Q: Does the application work on mobile devices?**
A: The interface is responsive and works on tablets, but desktop is recommended for best experience.

**Q: Can I integrate this with other systems?**
A: The application has API endpoints that can be integrated with other systems.

**Q: Is there an API documentation?**
A: Yes, technical documentation is available for developers.

---

## 10. Glossary

### Technical Terms

**API (Application Programming Interface)**
: A set of protocols and tools for building software applications, allowing different programs to communicate.

**Bearer Token**
: A security token that grants access to an API. Included in request headers for authentication.

**Configuration**
: A set of parameters defining how to test a specific API endpoint, including URL, authentication, and request templates.

**Endpoint**
: A specific URL where an API receives requests and sends responses.

**JSON (JavaScript Object Notation)**
: A lightweight data format used for exchanging information between systems.

**Scenario**
: A test case representing a potential fraud situation, containing user profiles, transaction details, and risk indicators.

### Business Terms

**Fraud Detection**
: The process of identifying potentially fraudulent activities or transactions.

**Risk Score**
: A numerical value (1-10) indicating the likelihood that a transaction is fraudulent.

**Success Rate**
: The percentage of API requests that complete successfully without errors.

**Response Time**
: The time taken for an API to process a request and return a response.

**Watchlist**
: A list of known fraudulent entities, accounts, or patterns used for comparison.

### Application Terms

**Background Service**
: An automated process that runs continuously to test APIs without user intervention.

**Bulk Operations**
: Actions performed on multiple items simultaneously, such as testing many scenarios at once.

**Dashboard**
: The main interface showing system status, statistics, and quick access to features.

**Real-time Updates**
: Information that refreshes automatically as new data becomes available.

**Soft Deletion**
: Marking records as deleted without permanently removing them from the database.

---

## Need More Help?

If you need additional assistance:

1. **Review this manual thoroughly**
2. **Check the troubleshooting section**
3. **Contact your system administrator**
4. **Reach out to technical support**

**Remember**: Include specific error messages, screenshots, and detailed descriptions when seeking help.

---

*This user manual was created to provide comprehensive guidance for using the FraudDetectorWebApp. For technical documentation, please refer to the Developer Manual.*

**Document Version**: 1.0  
**Last Updated**: 29 August 2025  
**Next Review**: 29 November 2025
