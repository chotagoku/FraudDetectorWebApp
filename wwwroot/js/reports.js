// Reports and Analytics JavaScript for Fraud Detector Pro
// API base URL
const API_BASE = '/api';

// Global variables
let currentReportData = [];
let configurations = [];
let summaryStats = {};

// Chart variables - Enhanced with new charts
let charts = {
    successRate: null,
    responseTime: null,
    riskLevel: null,
    performanceTimeline: null,
    configPerformance: null,
    errorTrends: null,
    volumeHeatmap: null
};

// Heatmap view state
let heatmapView = 'hourly';

// Chart color palettes
const chartColors = {
    primary: ['#007bff', '#0056b3', '#004085'],
    success: ['#28a745', '#1e7e34', '#155724'],
    danger: ['#dc3545', '#bd2130', '#a71e2a'],
    warning: ['#ffc107', '#e0a800', '#b69500'],
    info: ['#17a2b8', '#138496', '#0c6674'],
    gradient: {
        blue: 'linear-gradient(45deg, #007bff, #0056b3)',
        green: 'linear-gradient(45deg, #28a745, #1e7e34)',
        red: 'linear-gradient(45deg, #dc3545, #bd2130)',
        yellow: 'linear-gradient(45deg, #ffc107, #e0a800)',
        purple: 'linear-gradient(45deg, #6f42c1, #5a32a3)'
    }
};

// Initialize page
document.addEventListener('DOMContentLoaded', function() {
    initializeEventListeners();
    loadInitialData();
});

// Initialize all event listeners
function initializeEventListeners() {
    // Report generation
    const generateBtn = document.getElementById('generateReportBtn');
    if (generateBtn) {
        generateBtn.addEventListener('click', generateReport);
    }

    // Export buttons
    const exportExcelBtn = document.getElementById('exportExcelBtn');
    if (exportExcelBtn) {
        exportExcelBtn.addEventListener('click', exportToExcel);
    }

    const exportJsonBtn = document.getElementById('exportJsonBtn');
    if (exportJsonBtn) {
        exportJsonBtn.addEventListener('click', exportToJson);
    }

    const printReportBtn = document.getElementById('printReportBtn');
    if (printReportBtn) {
        printReportBtn.addEventListener('click', printReport);
    }

    const scheduleReportBtn = document.getElementById('scheduleReportBtn');
    if (scheduleReportBtn) {
        scheduleReportBtn.addEventListener('click', scheduleReport);
    }

    // Chart controls
    const refreshChartsBtn = document.getElementById('refreshChartsBtn');
    if (refreshChartsBtn) {
        refreshChartsBtn.addEventListener('click', refreshCharts);
    }

    const exportChartsBtn = document.getElementById('exportChartsBtn');
    if (exportChartsBtn) {
        exportChartsBtn.addEventListener('click', exportCharts);
    }

    // Data refresh
    const refreshDataBtn = document.getElementById('refreshDataBtn');
    if (refreshDataBtn) {
        refreshDataBtn.addEventListener('click', refreshData);
    }
}

// Load initial data
async function loadInitialData() {
    showLoadingState();
    await Promise.all([
        loadSummaryStats(),
        loadConfigurations(),
        loadChartsData()
    ]);
    hideLoadingState();
}

// Load summary statistics
async function loadSummaryStats() {
    try {
        const response = await fetch(`${API_BASE}/results/statistics`);
        if (response.ok) {
            summaryStats = await response.json();
            renderSummaryStats();
        } else {
            console.warn('Failed to load summary statistics');
            summaryStats = {
                totalScenarios: 0,
                totalApiTests: 0,
                totalConfigurations: 0,
                averageResponseTime: 0
            };
            renderSummaryStats();
        }
    } catch (error) {
        console.error('Error loading summary stats:', error);
        summaryStats = {
            totalScenarios: 0,
            totalApiTests: 0,
            totalConfigurations: 0,
            averageResponseTime: 0
        };
        renderSummaryStats();
    }
}

// Load configurations
async function loadConfigurations() {
    try {
        const response = await fetch(`${API_BASE}/configuration`);
        if (response.ok) {
            configurations = await response.json();
            populateConfigurationFilter();
        } else {
            console.warn('Failed to load configurations');
            configurations = [];
            populateConfigurationFilter();
        }
    } catch (error) {
        console.error('Error loading configurations:', error);
        configurations = [];
        populateConfigurationFilter();
    }
}

// Render summary statistics
function renderSummaryStats() {
    const elements = {
        totalScenarios: document.getElementById('totalScenarios'),
        totalApiTests: document.getElementById('totalApiTests'), 
        totalConfigurations: document.getElementById('totalConfigurations'),
        avgResponseTime: document.getElementById('avgResponseTime')
    };

    // Map API response fields to UI elements
    // summaryStats comes from /api/results/statistics
    if (elements.totalScenarios) {
        // Use total requests as scenarios for now
        elements.totalScenarios.textContent = summaryStats.totalRequests || 0;
    }
    if (elements.totalApiTests) {
        // Use successful + failed requests as total API tests
        elements.totalApiTests.textContent = summaryStats.totalRequests || 0;
    }
    if (elements.totalConfigurations) {
        // Use configurations count from API response
        elements.totalConfigurations.textContent = summaryStats.configurations || 0;
    }
    if (elements.avgResponseTime) {
        const avgMs = Math.round(summaryStats.averageResponseTime || 0);
        elements.avgResponseTime.textContent = `${avgMs}ms`;
    }
}

// Populate configuration filter dropdown
function populateConfigurationFilter() {
    const configSelect = document.getElementById('configurationFilter');
    if (!configSelect) return;

    const options = ['<option value="">All Configurations</option>'];
    configurations.forEach(config => {
        options.push(`<option value="${config.id}">${config.name}</option>`);
    });
    
    configSelect.innerHTML = options.join('');
}

// Generate report based on selected filters
async function generateReport() {
    const reportType = document.getElementById('reportType')?.value || 'scenarios';
    const configId = document.getElementById('configurationFilter')?.value || '';
    const riskLevel = document.getElementById('riskLevelFilter')?.value || '';
    const dateRange = document.getElementById('dateRangeFilter')?.value || 'month';

    showLoadingState();

    try {
        let endpoint = `${API_BASE}/results`;
        const params = new URLSearchParams();
        
        if (configId) params.append('configId', configId);
        if (riskLevel) params.append('riskLevel', riskLevel);
        if (dateRange !== 'all') params.append('dateRange', dateRange);
        
        // Adjust page size based on report type
        params.append('pageSize', '1000');
        params.append('page', '1');

        const queryString = params.toString();
        if (queryString) {
            endpoint += `?${queryString}`;
        }

        const response = await fetch(endpoint);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const data = await response.json();
        currentReportData = Array.isArray(data) ? data : data.results || [];

        renderReportData(reportType);
        await loadChartsData(); // Refresh charts with filtered data

    } catch (error) {
        console.error('Error generating report:', error);
        showToast('Failed to generate report. Please try again.', 'error');
        renderEmptyReport();
    } finally {
        hideLoadingState();
    }
}

// Render report data
function renderReportData(reportType) {
    const reportContent = document.getElementById('reportContent');
    if (!reportContent) return;

    if (currentReportData.length === 0) {
        renderEmptyReport();
        return;
    }

    const reportCount = document.getElementById('reportDataCount');
    if (reportCount) {
        reportCount.textContent = currentReportData.length;
    }

    let html = '';
    
    switch (reportType) {
        case 'scenarios':
            html = generateScenariosReport();
            break;
        case 'configuration':
            html = generateConfigurationReport();
            break;
        default:
            html = generateScenariosReport();
    }

    reportContent.innerHTML = html;
}

// Generate scenarios report
function generateScenariosReport() {
    if (currentReportData.length === 0) {
        return '<div class="text-center py-4"><p class="text-muted">No scenario data available</p></div>';
    }

    const table = `
        <div class="table-responsive">
            <table class="table table-striped table-hover">
                <thead class="table-dark">
                    <tr>
                        <th>Configuration</th>
                        <th>Iteration</th>
                        <th>Timestamp</th>
                        <th>Response Time</th>
                        <th>Status</th>
                        <th>Success</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    ${currentReportData.map(result => `
                        <tr>
                            <td>${result.name || 'Unknown'}</td>
                            <td>#${result.iterationNumber || 0}</td>
                            <td>${new Date(result.requestTimestamp).toLocaleString()}</td>
                            <td><span class="response-time">${result.responseTimeMs || 0}ms</span></td>
                            <td><span class="badge ${result.statusCode >= 200 && result.statusCode < 300 ? 'bg-success' : 'bg-danger'}">${result.statusCode || 'N/A'}</span></td>
                            <td><i class="fas ${result.isSuccessful ? 'fa-check-circle text-success' : 'fa-times-circle text-danger'}"></i></td>
                            <td>
                                <button class="btn btn-sm btn-outline-info" onclick="viewResultDetails(${result.id})" title="View Details">
                                    <i class="fas fa-eye"></i>
                                </button>
                            </td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        </div>
    `;

    return table;
}

// Generate configuration report
function generateConfigurationReport() {
    if (configurations.length === 0) {
        return '<div class="text-center py-4"><p class="text-muted">No configuration data available</p></div>';
    }

    const table = `
        <div class="table-responsive">
            <table class="table table-striped table-hover">
                <thead class="table-dark">
                    <tr>
                        <th>Name</th>
                        <th>Endpoint</th>
                        <th>Status</th>
                        <th>Delay (ms)</th>
                        <th>Max Iterations</th>
                        <th>Total Requests</th>
                        <th>Last Request</th>
                    </tr>
                </thead>
                <tbody>
                    ${configurations.map(config => `
                        <tr>
                            <td>${config.name}</td>
                            <td><code>${config.apiEndpoint}</code></td>
                            <td><span class="badge ${config.isActive ? 'bg-success' : 'bg-secondary'}">${config.isActive ? 'Running' : 'Stopped'}</span></td>
                            <td>${config.delayBetweenRequests}ms</td>
                            <td>${config.maxIterations || '∞'}</td>
                            <td>${config.requestLogsCount || 0}</td>
                            <td>${config.lastRequestTime ? new Date(config.lastRequestTime).toLocaleString() : 'Never'}</td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        </div>
    `;

    return table;
}

// Render empty report
function renderEmptyReport() {
    const reportContent = document.getElementById('reportContent');
    if (reportContent) {
        reportContent.innerHTML = `
            <div class="text-center py-5">
                <i class="fas fa-chart-line fa-3x text-muted mb-3"></i>
                <h5 class="text-muted">No Data Available</h5>
                <p class="text-muted">Select filters and click "Generate Report" to view detailed analytics.</p>
            </div>
        `;
    }

    const reportCount = document.getElementById('reportDataCount');
    if (reportCount) {
        reportCount.textContent = '0';
    }
}

// Show loading state
function showLoadingState() {
    const reportContent = document.getElementById('reportContent');
    if (reportContent) {
        reportContent.innerHTML = `
            <div class="text-center py-5">
                <div class="spinner-border text-primary mb-3" role="status"></div>
                <h5 class="text-muted">Loading Report Data...</h5>
                <p class="text-muted">Please wait while we fetch the latest analytics.</p>
            </div>
        `;
    }
}

// Hide loading state
function hideLoadingState() {
    // Loading state will be replaced by report content
}

// Export to Excel/CSV
function exportToExcel() {
    if (currentReportData.length === 0) {
        showToast('No data to export. Generate a report first.', 'error');
        return;
    }

    const reportType = document.getElementById('reportType')?.value || 'report';
    const filename = `${reportType}_report_${new Date().toISOString().split('T')[0]}.csv`;
    
    // Get headers from first data object
    const headers = currentReportData.length > 0 ? Object.keys(currentReportData[0]) : [];
    
    const csvContent = [
        headers.join(','),
        ...currentReportData.map(row => 
            headers.map(header => {
                const value = row[header] || '';
                return `"${String(value).replace(/"/g, '""')}"`;
            }).join(',')
        )
    ].join('\n');
    
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    showToast('Excel file exported successfully!');
}

// Export to JSON
function exportToJson() {
    if (currentReportData.length === 0) {
        showToast('No data to export. Generate a report first.', 'error');
        return;
    }
    
    const reportType = document.getElementById('reportType')?.value || 'report';
    const filename = `${reportType}_report_${new Date().toISOString().split('T')[0]}.json`;
    
    const jsonData = JSON.stringify({
        reportType: reportType,
        generatedAt: new Date().toISOString(),
        totalRecords: currentReportData.length,
        data: currentReportData
    }, null, 2);
    
    const blob = new Blob([jsonData], { type: 'application/json' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    showToast('JSON file exported successfully!');
}

// Print report
function printReport() {
    if (currentReportData.length === 0) {
        showToast('No data to print. Generate a report first.', 'error');
        return;
    }
    
    const reportContent = document.getElementById('reportContent').innerHTML;
    const printWindow = window.open('', '_blank');
    printWindow.document.write(`
        <html>
            <head>
                <title>Fraud Detector Pro - Analytics Report</title>
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
                <style>
                    body { font-family: Arial, sans-serif; }
                    .response-time { font-weight: bold; }
                    @media print {
                        .btn { display: none; }
                        .no-print { display: none; }
                    }
                </style>
            </head>
            <body>
                <div class="container mt-4">
                    <div class="text-center mb-4">
                        <h2>Fraud Detector Pro</h2>
                        <h3>Analytics & Reports</h3>
                        <p class="text-muted">Generated on ${new Date().toLocaleString()}</p>
                    </div>
                    ${reportContent}
                    <div class="mt-4 text-center">
                        <small class="text-muted">
                            © 2025 Fraud Detector Pro. All rights reserved.<br>
                            Built with ASP.NET Core & Bootstrap
                        </small>
                    </div>
                </div>
            </body>
        </html>
    `);
    printWindow.document.close();
    printWindow.print();
}

// Schedule report (placeholder)
function scheduleReport() {
    showToast('Report scheduling feature coming soon!', 'info');
}

// Refresh all data
async function refreshData() {
    showLoadingState();
    await Promise.all([
        loadSummaryStats(),
        loadConfigurations()
    ]);
    
    if (currentReportData.length > 0) {
        generateReport();
    } else {
        document.getElementById('reportContent').innerHTML = `
            <div class="text-center py-5">
                <i class="fas fa-chart-line fa-3x text-muted mb-3"></i>
                <h5 class="text-muted">Data refreshed successfully</h5>
                <p class="text-muted">Select filters and click "Generate Report" to view analytics.</p>
            </div>
        `;
    }
}

// Load charts data and initialize charts
async function loadChartsData() {
    try {
        // Fetch multiple data sources
        // Note: scenarios endpoint requires authentication, so we handle it gracefully
        const [resultsResp, configsResp] = await Promise.all([
            fetch('/api/results?pageSize=1000&page=1'),
            fetch('/api/configuration')
        ]);
        
        const apiLogs = resultsResp.ok ? await resultsResp.json() : [];
        const configs = configsResp.ok ? await configsResp.json() : [];
        
        // Try to fetch scenarios, but handle authorization gracefully
        let scenarios = [];
        try {
            const scenariosResp = await fetch('/api/generations?pageSize=1000&page=1');
            if (scenariosResp.ok) {
                scenarios = await scenariosResp.json();
            } else if (scenariosResp.status === 401 || scenariosResp.status === 403) {
                console.log('Scenarios endpoint requires authentication - using API logs data only');
            }
        } catch (scenarioError) {
            console.log('Could not fetch scenarios data:', scenarioError.message);
        }
        
        const logsArray = Array.isArray(apiLogs) ? apiLogs : apiLogs.results || [];
        console.log('Chart data loaded:', logsArray.length, 'API logs,', configs.length, 'configs,', scenarios.length, 'scenarios');
        
        // Process data for charts
        const chartData = processAdvancedChartData(logsArray, configs, scenarios);
        
        // Initialize all charts
        initializeSuccessRateChart(chartData.successRate);
        initializeResponseTimeChart(chartData.responseTime);
        initializeRiskLevelChart(chartData.riskLevel);
        initializePerformanceTimelineChart(chartData.timeline);
        initializeConfigPerformanceChart(chartData.configPerformance);
        initializeErrorTrendsChart(chartData.errorTrends);
        initializeVolumeHeatmapChart(chartData.volumeHeatmap);
        
        // Update metric badges
        updateMetricBadges(chartData);
        
    } catch (error) {
        console.error('Error loading charts data:', error);
        initializeEmptyCharts();
    }
}

// Process raw API logs into chart-ready data
function processChartData(apiLogs) {
    // Success Rate Data
    const totalLogs = apiLogs.length;
    const successfulLogs = apiLogs.filter(log => log.isSuccessful).length;
    const failedLogs = totalLogs - successfulLogs;
    
    // Response Time Distribution
    const responseTimes = apiLogs.map(log => log.responseTimeMs || 0);
    const responseTimeBuckets = {
        '0-1000ms': responseTimes.filter(t => t >= 0 && t < 1000).length,
        '1-3s': responseTimes.filter(t => t >= 1000 && t < 3000).length,
        '3-5s': responseTimes.filter(t => t >= 3000 && t < 5000).length,
        '5-10s': responseTimes.filter(t => t >= 5000 && t < 10000).length,
        '10s+': responseTimes.filter(t => t >= 10000).length
    };
    
    // Risk Level Distribution (from request payloads)
    const riskCounts = { low: 0, medium: 0, high: 0, unknown: 0 };
    apiLogs.forEach(log => {
        try {
            const payload = JSON.parse(log.requestPayload || '{}');
            if (payload.messages && payload.messages[0] && payload.messages[0].content) {
                const content = payload.messages[0].content;
                const riskMatch = content.match(/Amount Risk Score: (\d+)/);
                if (riskMatch) {
                    const score = parseInt(riskMatch[1]);
                    if (score <= 3) riskCounts.low++;
                    else if (score <= 6) riskCounts.medium++;
                    else riskCounts.high++;
                } else {
                    riskCounts.unknown++;
                }
            } else {
                riskCounts.unknown++;
            }
        } catch (e) {
            riskCounts.unknown++;
        }
    });
    
    // Performance Timeline (hourly buckets for recent data)
    const timelineBuckets = {};
    const now = new Date();
    for (let i = 23; i >= 0; i--) {
        const hour = new Date(now.getTime() - (i * 60 * 60 * 1000));
        const key = hour.getHours().toString().padStart(2, '0') + ':00';
        timelineBuckets[key] = { successful: 0, failed: 0, avgResponse: 0 };
    }
    
    // Group logs by hour
    const hourlyGroups = {};
    apiLogs.forEach(log => {
        const logTime = new Date(log.requestTimestamp);
        const hourKey = logTime.getHours().toString().padStart(2, '0') + ':00';
        
        if (!hourlyGroups[hourKey]) {
            hourlyGroups[hourKey] = [];
        }
        hourlyGroups[hourKey].push(log);
    });
    
    // Calculate hourly statistics
    Object.keys(hourlyGroups).forEach(hour => {
        const logs = hourlyGroups[hour];
        const successful = logs.filter(l => l.isSuccessful).length;
        const failed = logs.length - successful;
        const avgResponse = logs.length > 0 ? 
            Math.round(logs.reduce((sum, l) => sum + (l.responseTimeMs || 0), 0) / logs.length) : 0;
            
        if (timelineBuckets[hour]) {
            timelineBuckets[hour] = { successful, failed, avgResponse };
        }
    });
    
    return {
        successRate: {
            successful: successfulLogs,
            failed: failedLogs
        },
        responseTime: responseTimeBuckets,
        riskLevel: riskCounts,
        timeline: timelineBuckets
    };
}

// Initialize Success Rate Chart
function initializeSuccessRateChart(data) {
    const ctx = document.getElementById('successRateChart')?.getContext('2d');
    if (!ctx) return;
    
    if (charts.successRate) {
        charts.successRate.destroy();
    }
    
    charts.successRate = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Successful', 'Failed'],
            datasets: [{
                data: [data.successful, data.failed],
                backgroundColor: ['#28a745', '#dc3545'],
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const total = data.successful + data.failed;
                            const percentage = total > 0 ? Math.round((context.parsed / total) * 100) : 0;
                            return `${context.label}: ${context.parsed} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
}

// Initialize Response Time Chart
function initializeResponseTimeChart(data) {
    const ctx = document.getElementById('responseTimeChart')?.getContext('2d');
    if (!ctx) return;
    
    if (charts.responseTime) {
        charts.responseTime.destroy();
    }
    
    charts.responseTime = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: Object.keys(data),
            datasets: [{
                label: 'Number of Requests',
                data: Object.values(data),
                backgroundColor: ['#007bff', '#17a2b8', '#ffc107', '#fd7e14', '#dc3545'],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });
}

// Initialize Risk Level Chart
function initializeRiskLevelChart(data) {
    const ctx = document.getElementById('riskLevelChart')?.getContext('2d');
    if (!ctx) return;
    
    if (charts.riskLevel) {
        charts.riskLevel.destroy();
    }
    
    charts.riskLevel = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['Low Risk', 'Medium Risk', 'High Risk', 'Unknown'],
            datasets: [{
                data: [data.low, data.medium, data.high, data.unknown],
                backgroundColor: ['#28a745', '#ffc107', '#dc3545', '#6c757d'],
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom'
                }
            }
        }
    });
}

// Initialize Performance Timeline Chart
function initializePerformanceTimelineChart(data) {
    const ctx = document.getElementById('performanceTimelineChart')?.getContext('2d');
    if (!ctx) return;
    
    if (charts.performanceTimeline) {
        charts.performanceTimeline.destroy();
    }
    
    const labels = Object.keys(data);
    const successData = labels.map(hour => data[hour].successful);
    const failedData = labels.map(hour => data[hour].failed);
    const responseData = labels.map(hour => data[hour].avgResponse);
    
    charts.performanceTimeline = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Successful Requests',
                    data: successData,
                    borderColor: '#28a745',
                    backgroundColor: 'rgba(40, 167, 69, 0.1)',
                    yAxisID: 'y'
                },
                {
                    label: 'Failed Requests',
                    data: failedData,
                    borderColor: '#dc3545',
                    backgroundColor: 'rgba(220, 53, 69, 0.1)',
                    yAxisID: 'y'
                },
                {
                    label: 'Avg Response Time (ms)',
                    data: responseData,
                    borderColor: '#ffc107',
                    backgroundColor: 'rgba(255, 193, 7, 0.1)',
                    type: 'line',
                    yAxisID: 'y1'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false,
            },
            scales: {
                x: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Hour'
                    }
                },
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    title: {
                        display: true,
                        text: 'Request Count'
                    },
                    beginAtZero: true
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    title: {
                        display: true,
                        text: 'Response Time (ms)'
                    },
                    beginAtZero: true,
                    grid: {
                        drawOnChartArea: false,
                    },
                }
            },
            plugins: {
                legend: {
                    position: 'bottom'
                }
            }
        }
    });
}

// Process advanced chart data with multiple data sources
function processAdvancedChartData(apiLogs, configs, scenarios) {
    const baseData = processChartData(apiLogs);
    
    // Configuration Performance Data
    const configPerformance = configs.map(config => {
        const configLogs = apiLogs.filter(log => log.apiConfigurationId === config.id);
        const successRate = configLogs.length > 0 ? 
            (configLogs.filter(log => log.isSuccessful).length / configLogs.length) * 100 : 0;
        const avgResponse = configLogs.length > 0 ? 
            configLogs.reduce((sum, log) => sum + (log.responseTimeMs || 0), 0) / configLogs.length : 0;
        
        return {
            name: config.name,
            successRate: Math.round(successRate),
            avgResponse: Math.round(avgResponse),
            totalRequests: configLogs.length
        };
    });
    
    // Error Trends Data (last 7 days)
    const errorTrends = [];
    const now = new Date();
    for (let i = 6; i >= 0; i--) {
        const date = new Date(now.getTime() - (i * 24 * 60 * 60 * 1000));
        const dayStart = new Date(date.getFullYear(), date.getMonth(), date.getDate());
        const dayEnd = new Date(dayStart.getTime() + 24 * 60 * 60 * 1000);
        
        const dayLogs = apiLogs.filter(log => {
            const logDate = new Date(log.requestTimestamp);
            return logDate >= dayStart && logDate < dayEnd;
        });
        
        const total = dayLogs.length;
        const errors = dayLogs.filter(log => !log.isSuccessful).length;
        const errorRate = total > 0 ? (errors / total) * 100 : 0;
        
        errorTrends.push({
            date: date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' }),
            errorRate: Math.round(errorRate * 100) / 100,
            total: total,
            errors: errors
        });
    }
    
    // Volume Heatmap Data (7 days x 24 hours)
    const volumeHeatmap = [];
    for (let day = 6; day >= 0; day--) {
        const dayData = [];
        for (let hour = 0; hour < 24; hour++) {
            const targetTime = new Date(now.getTime() - (day * 24 * 60 * 60 * 1000));
            targetTime.setHours(hour, 0, 0, 0);
            const nextHour = new Date(targetTime.getTime() + 60 * 60 * 1000);
            
            const hourLogs = apiLogs.filter(log => {
                const logTime = new Date(log.requestTimestamp);
                return logTime >= targetTime && logTime < nextHour;
            });
            
            dayData.push({
                day: targetTime.toLocaleDateString('en-US', { weekday: 'short' }),
                hour: hour,
                volume: hourLogs.length,
                success: hourLogs.filter(log => log.isSuccessful).length,
                failed: hourLogs.filter(log => !log.isSuccessful).length
            });
        }
        volumeHeatmap.push(dayData);
    }
    
    return {
        ...baseData,
        configPerformance,
        errorTrends,
        volumeHeatmap
    };
}

// Initialize Configuration Performance Chart
function initializeConfigPerformanceChart(data) {
    const ctx = document.getElementById('configPerformanceChart')?.getContext('2d');
    if (!ctx) return;
    
    if (charts.configPerformance) {
        charts.configPerformance.destroy();
    }
    
    if (data.length === 0) {
        charts.configPerformance = new Chart(ctx, {
            type: 'radar',
            data: {
                labels: ['No Data'],
                datasets: [{
                    label: 'No configurations available',
                    data: [0],
                    borderColor: '#6c757d',
                    backgroundColor: 'rgba(108, 117, 125, 0.2)'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
        return;
    }
    
    charts.configPerformance = new Chart(ctx, {
        type: 'radar',
        data: {
            labels: data.map(config => config.name),
            datasets: [
                {
                    label: 'Success Rate (%)',
                    data: data.map(config => config.successRate),
                    borderColor: chartColors.success[0],
                    backgroundColor: 'rgba(40, 167, 69, 0.2)',
                    pointBackgroundColor: chartColors.success[0]
                },
                {
                    label: 'Response Speed Score',
                    data: data.map(config => Math.max(0, 100 - (config.avgResponse / 10))),
                    borderColor: chartColors.info[0],
                    backgroundColor: 'rgba(23, 162, 184, 0.2)',
                    pointBackgroundColor: chartColors.info[0]
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                r: {
                    beginAtZero: true,
                    max: 100,
                    ticks: {
                        stepSize: 20
                    }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            if (context.dataset.label === 'Success Rate (%)') {
                                return `${context.dataset.label}: ${context.parsed.r}%`;
                            } else {
                                const config = data[context.dataIndex];
                                return `Response Time: ${config.avgResponse}ms (Score: ${Math.round(context.parsed.r)})`;
                            }
                        }
                    }
                }
            }
        }
    });
}

// Initialize Error Trends Chart
function initializeErrorTrendsChart(data) {
    const ctx = document.getElementById('errorTrendsChart')?.getContext('2d');
    if (!ctx) return;
    
    if (charts.errorTrends) {
        charts.errorTrends.destroy();
    }
    
    const gradient = ctx.createLinearGradient(0, 0, 0, 400);
    gradient.addColorStop(0, 'rgba(220, 53, 69, 0.4)');
    gradient.addColorStop(1, 'rgba(220, 53, 69, 0.1)');
    
    charts.errorTrends = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(day => day.date),
            datasets: [
                {
                    label: 'Error Rate (%)',
                    data: data.map(day => day.errorRate),
                    borderColor: chartColors.danger[0],
                    backgroundColor: gradient,
                    fill: true,
                    tension: 0.4,
                    pointRadius: 6,
                    pointHoverRadius: 8,
                    pointBackgroundColor: '#fff',
                    pointBorderColor: chartColors.danger[0],
                    pointBorderWidth: 2
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                intersect: false,
                mode: 'index'
            },
            scales: {
                x: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Date'
                    }
                },
                y: {
                    beginAtZero: true,
                    max: Math.max(10, Math.max(...data.map(d => d.errorRate)) * 1.2),
                    title: {
                        display: true,
                        text: 'Error Rate (%)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value + '%';
                        }
                    }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const day = data[context.dataIndex];
                            return [
                                `Error Rate: ${day.errorRate}%`,
                                `Failed Requests: ${day.errors}`,
                                `Total Requests: ${day.total}`
                            ];
                        }
                    }
                }
            }
        }
    });
}

// Initialize Volume Heatmap Chart
function initializeVolumeHeatmapChart(data) {
    const ctx = document.getElementById('volumeHeatmapChart')?.getContext('2d');
    if (!ctx) return;
    
    if (charts.volumeHeatmap) {
        charts.volumeHeatmap.destroy();
    }
    
    // Flatten the 2D heatmap data for Chart.js
    const flatData = [];
    const labels = [];
    
    data.forEach((dayData, dayIndex) => {
        dayData.forEach((hourData, hourIndex) => {
            flatData.push({
                x: hourIndex,
                y: dayIndex,
                v: hourData.volume
            });
        });
    });
    
    // Create hour labels (0:00 to 23:00)
    const hourLabels = [];
    for (let i = 0; i < 24; i++) {
        hourLabels.push(`${i.toString().padStart(2, '0')}:00`);
    }
    
    // Create day labels
    const dayLabels = data.length > 0 ? data[0].map(hour => hour.day) : [];
    
    charts.volumeHeatmap = new Chart(ctx, {
        type: 'scatter',
        data: {
            datasets: [{
                label: 'Request Volume',
                data: flatData,
                backgroundColor: function(context) {
                    const value = context.parsed.v;
                    if (value === 0) return 'rgba(206, 212, 218, 0.3)';
                    if (value <= 5) return 'rgba(40, 167, 69, 0.4)';
                    if (value <= 15) return 'rgba(255, 193, 7, 0.6)';
                    if (value <= 30) return 'rgba(253, 126, 20, 0.7)';
                    return 'rgba(220, 53, 69, 0.8)';
                },
                borderColor: '#fff',
                borderWidth: 1,
                pointRadius: function(context) {
                    const value = context.parsed.v;
                    return Math.max(3, Math.min(15, value / 2 + 3));
                }
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    type: 'linear',
                    position: 'bottom',
                    min: 0,
                    max: 23,
                    title: {
                        display: true,
                        text: 'Hour of Day'
                    },
                    ticks: {
                        stepSize: 2,
                        callback: function(value) {
                            return `${value.toString().padStart(2, '0')}:00`;
                        }
                    }
                },
                y: {
                    type: 'linear',
                    min: 0,
                    max: Math.max(1, data.length - 1),
                    title: {
                        display: true,
                        text: 'Day'
                    },
                    ticks: {
                        stepSize: 1,
                        callback: function(value) {
                            return dayLabels[Math.round(value)] || '';
                        }
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        title: function(context) {
                            const point = context[0];
                            const hour = Math.round(point.parsed.x);
                            const day = Math.round(point.parsed.y);
                            return `${dayLabels[day]} at ${hour.toString().padStart(2, '0')}:00`;
                        },
                        label: function(context) {
                            return `Requests: ${context.parsed.v}`;
                        }
                    }
                }
            }
        }
    });
}

// Update metric badges
function updateMetricBadges(chartData) {
    // Success Rate Metric
    const totalRequests = chartData.successRate.successful + chartData.successRate.failed;
    const successPercentage = totalRequests > 0 ? 
        Math.round((chartData.successRate.successful / totalRequests) * 100) : 0;
    const successMetric = document.getElementById('successRateMetric');
    if (successMetric) {
        successMetric.textContent = `${successPercentage}%`;
        successMetric.className = `badge ${successPercentage >= 80 ? 'bg-success' : successPercentage >= 50 ? 'bg-warning' : 'bg-danger'} text-white`;
    }
    
    // Average Response Time Metric
    const avgResponse = chartData.timeline ? 
        Object.values(chartData.timeline).reduce((sum, hour) => sum + hour.avgResponse, 0) / Object.keys(chartData.timeline).length : 0;
    const avgMetric = document.getElementById('avgResponseMetric');
    if (avgMetric) {
        avgMetric.textContent = `${Math.round(avgResponse)}ms`;
        avgMetric.className = `badge ${avgResponse <= 1000 ? 'bg-success' : avgResponse <= 3000 ? 'bg-warning' : 'bg-danger'} text-white`;
    }
    
    // Risk Metric
    const totalRisk = chartData.riskLevel.low + chartData.riskLevel.medium + chartData.riskLevel.high + chartData.riskLevel.unknown;
    const riskMetric = document.getElementById('riskMetric');
    if (riskMetric) {
        riskMetric.textContent = `${totalRisk} scenarios`;
        const highRiskPercentage = totalRisk > 0 ? (chartData.riskLevel.high / totalRisk) * 100 : 0;
        riskMetric.className = `badge ${highRiskPercentage <= 20 ? 'bg-success' : highRiskPercentage <= 50 ? 'bg-warning' : 'bg-danger'} text-white`;
    }
    
    // Timeline Metric
    const timelineTotal = chartData.timeline ? 
        Object.values(chartData.timeline).reduce((sum, hour) => sum + hour.successful + hour.failed, 0) : 0;
    const timelineMetric = document.getElementById('timelineMetric');
    if (timelineMetric) {
        timelineMetric.textContent = `${timelineTotal} requests`;
        timelineMetric.className = 'badge bg-light text-dark';
    }
    
    // Config Metric
    const configMetric = document.getElementById('configMetric');
    if (configMetric) {
        configMetric.textContent = `${chartData.configPerformance.length} configs`;
        configMetric.className = 'badge bg-light text-dark';
    }
    
    // Error Metric
    const avgErrorRate = chartData.errorTrends.length > 0 ? 
        chartData.errorTrends.reduce((sum, day) => sum + day.errorRate, 0) / chartData.errorTrends.length : 0;
    const errorMetric = document.getElementById('errorMetric');
    if (errorMetric) {
        errorMetric.textContent = `${Math.round(avgErrorRate * 100) / 100}% errors`;
        errorMetric.className = `badge ${avgErrorRate <= 5 ? 'bg-success' : avgErrorRate <= 15 ? 'bg-warning' : 'bg-danger'} text-white`;
    }
}

// Set heatmap view
function setHeatmapView(view) {
    heatmapView = view;
    
    // Update button states
    document.getElementById('heatmapDaily').classList.toggle('active', view === 'daily');
    document.getElementById('heatmapHourly').classList.toggle('active', view === 'hourly');
    
    // Refresh heatmap chart
    loadChartsData();
}

// Initialize empty charts when no data is available
function initializeEmptyCharts() {
    initializeSuccessRateChart({ successful: 0, failed: 0 });
    initializeResponseTimeChart({ '0-1000ms': 0, '1-3s': 0, '3-5s': 0, '5-10s': 0, '10s+': 0 });
    initializeRiskLevelChart({ low: 0, medium: 0, high: 0, unknown: 0 });
    
    // Create empty timeline data
    const emptyTimeline = {};
    const now = new Date();
    for (let i = 23; i >= 0; i--) {
        const hour = new Date(now.getTime() - (i * 60 * 60 * 1000));
        const key = hour.getHours().toString().padStart(2, '0') + ':00';
        emptyTimeline[key] = { successful: 0, failed: 0, avgResponse: 0 };
    }
    initializePerformanceTimelineChart(emptyTimeline);
    
    // Initialize empty advanced charts
    initializeConfigPerformanceChart([]);
    initializeErrorTrendsChart([]);
    initializeVolumeHeatmapChart([]);
}

// Refresh charts
async function refreshCharts() {
    await loadChartsData();
    showToast('Charts refreshed successfully!');
}

// Export charts as images
function exportCharts() {
    const timestamp = new Date().toISOString().split('T')[0];
    
    Object.keys(charts).forEach(chartName => {
        if (charts[chartName]) {
            const canvas = charts[chartName].canvas;
            const link = document.createElement('a');
            link.download = `${chartName}_chart_${timestamp}.png`;
            link.href = canvas.toDataURL();
            link.click();
        }
    });
    
    showToast('Charts exported successfully!');
}

// View result details (shared function from dashboard)
async function viewResultDetails(resultId) {
    try {
        const response = await fetch(`${API_BASE}/results/${resultId}`);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const result = await response.json();
        
        const details = `
            <div class="row">
                <div class="col-md-6">
                    <h6>Request Information</h6>
                    <p><strong>Configuration:</strong> ${result.name || result.apiConfiguration?.name || 'Unknown'}</p>
                    <p><strong>Iteration:</strong> ${result.iterationNumber}</p>
                    <p><strong>Timestamp:</strong> ${new Date(result.requestTimestamp).toLocaleString()}</p>
                    <p><strong>Status Code:</strong> ${result.statusCode}</p>
                    <p><strong>Response Time:</strong> <span class="response-time">${result.responseTimeMs}ms</span></p>
                    <p><strong>Success:</strong> 
                        <span class="${result.isSuccessful ? 'text-success' : 'text-danger'}">
                            ${result.isSuccessful ? 'Yes' : 'No'}
                        </span>
                    </p>
                    ${result.errorMessage ? `<p><strong>Error:</strong> <span class="text-danger">${result.errorMessage}</span></p>` : ''}
                </div>
                <div class="col-md-6">
                    <h6>Request Payload</h6>
                    <div class="json-display">
                        <pre>${JSON.stringify(JSON.parse(result.requestPayload || '{}'), null, 2)}</pre>
                    </div>
                </div>
            </div>
            ${result.responseContent ? `
                <div class="row mt-3">
                    <div class="col-12">
                        <h6>Response Content</h6>
                        <div class="json-display">
                            <pre>${JSON.stringify(JSON.parse(result.responseContent), null, 2)}</pre>
                        </div>
                    </div>
                </div>
            ` : ''}
        `;
        
        document.getElementById('resultDetails').innerHTML = details;
        new bootstrap.Modal(document.getElementById('resultModal')).show();
    } catch (error) {
        console.error('Error loading result details:', error);
        showToast('Failed to load result details.', 'error');
    }
}

// Show toast notification
function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    const alertType = type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info';
    toast.className = 'alert alert-' + alertType + ' position-fixed';
    toast.style.cssText = 'top: 80px; right: 20px; z-index: 9999; min-width: 250px;';
    const iconClass = type === 'success' ? 'check' : type === 'error' ? 'exclamation-triangle' : 'info-circle';
    toast.innerHTML = '<i class="fas fa-' + iconClass + ' me-1"></i>' + message;
    document.body.appendChild(toast);
    
    setTimeout(() => {
        if (toast.parentNode) {
            toast.remove();
        }
    }, 4000);
}

// Export current report (alias for exportToExcel)
function exportCurrentReport() {
    exportToExcel();
}

// Utility function to safely destroy charts
function destroyChart(chartName) {
    if (charts[chartName]) {
        try {
            charts[chartName].destroy();
            charts[chartName] = null;
        } catch (error) {
            console.warn(`Error destroying chart ${chartName}:`, error);
            charts[chartName] = null;
        }
    }
}

// Cleanup function for page unload
window.addEventListener('beforeunload', function() {
    Object.keys(charts).forEach(chartName => {
        destroyChart(chartName);
    });
});

// Make key functions available globally
window.generateReport = generateReport;
window.exportToExcel = exportToExcel;
window.exportToJson = exportToJson;
window.printReport = printReport;
window.scheduleReport = scheduleReport;
window.refreshData = refreshData;
window.refreshCharts = refreshCharts;
window.exportCharts = exportCharts;
window.exportCurrentReport = exportCurrentReport;
window.viewResultDetails = viewResultDetails;
window.setHeatmapView = setHeatmapView;
