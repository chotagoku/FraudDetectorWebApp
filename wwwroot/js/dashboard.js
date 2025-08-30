// API base URL
const API_BASE = '/api';

// Global variables
let configurations = [];
let statistics = {};
let signalRConnection = null;

// Initialize page
document.addEventListener('DOMContentLoaded', function() {
    // Clear any existing modal backdrops
    clearModalBackdrops();
    
    // Initialize event listeners first
    initializeEventListeners();
    
    // Then initialize SignalR and load data
    initializeSignalR();
    loadData();
    
    // Reduced refresh interval since we have real-time updates
    setInterval(loadData, 30000); // Refresh every 30 seconds as backup
});

// Initialize SignalR connection
async function initializeSignalR() {
    try {
        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/apitest")
            .build();

        // Handle new result notifications
        signalRConnection.on("NewResult", function (result) {
            addResultToLiveView(result);
            updateStatisticsCounters();
        });

        // Handle system status changes
        signalRConnection.on("SystemStatusChanged", function (status) {
            renderSystemStatus(status.isRunning);
        });

        // Start connection
        await signalRConnection.start();
        console.log("SignalR Connected");
        
        // Add connection status indicator
        showConnectionStatus(true);
    } catch (err) {
        console.error("Error starting SignalR connection:", err);
        showConnectionStatus(false);
        // Retry connection after 5 seconds
        setTimeout(initializeSignalR, 5000);
    }
}

// Load all data
async function loadData() {
    try {
        await Promise.all([
            loadConfigurations(),
            loadStatistics()
        ]);
    } catch (error) {
        console.error('Error loading data:', error);
    }
}

// Load configurations
async function loadConfigurations() {
    try {
        const response = await fetch(`${API_BASE}/configuration`);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        configurations = await response.json();
        renderConfigurations();
    } catch (error) {
        console.error('Error loading configurations:', error);
        // Show fallback UI
        configurations = [];
        renderConfigurations();
    }
}

// Load statistics
async function loadStatistics() {
    try {
        const [statsResponse, statusResponse] = await Promise.all([
            fetch(`${API_BASE}/results/statistics`),
            fetch(`${API_BASE}/configuration/status`)
        ]);
        
        if (statsResponse.ok) {
            statistics = await statsResponse.json();
        } else {
            console.warn('Failed to load statistics, using defaults');
            statistics = {
                totalRequests: 0,
                successfulRequests: 0,
                failedRequests: 0,
                averageResponseTime: 0,
                recentResults: []
            };
        }
        
        let systemStatus = { isRunning: false };
        if (statusResponse.ok) {
            systemStatus = await statusResponse.json();
        } else {
            console.warn('Failed to load system status, using default');
        }
        
        renderStatistics();
        renderSystemStatus(systemStatus.isRunning);
        renderRecentResults();
    } catch (error) {
        console.error('Error loading statistics:', error);
        // Render with default values
        statistics = {
            totalRequests: 0,
            successfulRequests: 0,
            failedRequests: 0,
            averageResponseTime: 0,
            recentResults: []
        };
        renderStatistics();
        renderSystemStatus(false);
        renderRecentResults();
    }
}

// Render configurations
function renderConfigurations() {
    const container = document.getElementById('configurationsList');
    if (configurations.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5">
                <i class="fas fa-plus-circle fa-3x text-muted mb-3"></i>
                <h6 class="text-muted">No configurations found</h6>
                <p class="text-muted">Add your first API configuration to get started!</p>
                <button class="btn btn-primary btn-custom" data-bs-toggle="modal" data-bs-target="#configModal">
                    <i class="fas fa-plus me-1"></i> Add Configuration
                </button>
            </div>
        `;
        return;
    }
    
    const html = configurations.map(config => {
        const isActive = config.isActive;
        const statusBadgeClass = isActive ? 'status-running' : 'status-stopped';
        const requestCount = config.requestLogsCount || 0;
        console.log('Processing config:', config.name, 'lastRequestTime:', config.lastRequestTime);
        const lastRequest = config.lastRequestTime ? 
            new Date(config.lastRequestTime).toLocaleString() : 'Never';
        console.log('Formatted lastRequest:', lastRequest);
        
        return `
            <div class="config-card card mb-3">
                <div class="card-body p-3">
                    <div class="d-flex justify-content-between align-items-start mb-3">
                        <div class="flex-grow-1">
                            <div class="d-flex align-items-center mb-2">
                                <h6 class="card-title mb-0 me-2">${config.name}</h6>
                                <span class="status-badge ${statusBadgeClass}">
                                    <i class="fas ${isActive ? 'fa-play' : 'fa-stop'} me-1"></i>
                                    ${isActive ? 'Running' : 'Stopped'}
                                </span>
                            </div>
                            <p class="text-muted small mb-1">
                                <i class="fas fa-link me-1"></i>${config.apiEndpoint}
                            </p>
                            <div class="row text-center mt-2">
                                <div class="col-4">
                                    <small class="text-muted d-block">Delay</small>
                                    <strong>${config.delayBetweenRequests}ms</strong>
                                </div>
                                <div class="col-4">
                                    <small class="text-muted d-block">Max Iter</small>
                                    <strong>${config.maxIterations || '∞'}</strong>
                                </div>
                                <div class="col-4">
                                    <small class="text-muted d-block">Requests</small>
                                    <strong>${requestCount}</strong>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="d-flex justify-content-between align-items-center">
                        <small class="text-muted">
                            <i class="fas fa-clock me-1"></i>Last: ${lastRequest}
                        </small>
                        <div class="btn-group btn-group-sm">
                            <button class="btn btn-outline-secondary" onclick="editConfiguration(${config.id})" title="Edit">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-outline-${isActive ? 'danger' : 'success'}" 
                                    onclick="${isActive ? 'stopConfiguration' : 'startConfiguration'}(${config.id})"
                                    title="${isActive ? 'Stop' : 'Start'}">
                                <i class="fas fa-${isActive ? 'stop' : 'play'}"></i>
                            </button>
                            <button class="btn btn-outline-info" onclick="viewResults(${config.id})" title="View Results">
                                <i class="fas fa-chart-bar"></i>
                            </button>
                            <button class="btn btn-outline-warning" onclick="clearConfigurationLogs(${config.id})" title="Clear Logs">
                                <i class="fas fa-trash-alt"></i>
                            </button>
                            <button class="btn btn-outline-danger" onclick="deleteConfiguration(${config.id})" title="Delete">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }).join('');
    
    container.innerHTML = html;
}

// Render statistics
function renderStatistics() {
    document.getElementById('totalRequests').textContent = statistics.totalRequests || 0;
    document.getElementById('successfulRequests').textContent = statistics.successfulRequests || 0;
    document.getElementById('failedRequests').textContent = statistics.failedRequests || 0;
    
    // Convert milliseconds to seconds for better readability
    const avgTimeMs = statistics.averageResponseTime || 0;
    const avgTimeSec = (avgTimeMs / 1000).toFixed(2);
    document.getElementById('avgResponseTime').textContent = `${avgTimeSec}s`;
}

// Render system status
function renderSystemStatus(isRunning) {
    const statusElement = document.getElementById('statusText');
    statusElement.className = isRunning ? 'status-running' : 'status-stopped';
    statusElement.textContent = isRunning ? 'System Running' : 'System Stopped';
    
    const systemStatusDiv = document.getElementById('systemStatus');
    systemStatusDiv.className = `alert ${isRunning ? 'alert-success' : 'alert-warning'}`;
}

// Render recent results
function renderRecentResults() {
    const container = document.getElementById('recentResults');
    const results = statistics.recentResults || [];
    
    if (results.length === 0) {
        container.innerHTML = `
            <div class="text-center py-4">
                <i class="fas fa-chart-line fa-2x text-muted mb-2"></i>
                <p class="text-muted">No results yet</p>
                <small class="text-muted">Results will appear here when tests start running</small>
            </div>
        `;
        return;
    }
    
    const html = results.map(result => {
        const isSuccess = result.isSuccessful;
        const resultClass = isSuccess ? 'result-success' : 'result-error';
        const statusIcon = isSuccess ? 'fa-check-circle' : 'fa-times-circle';
        const statusColor = isSuccess ? 'text-success' : 'text-danger';
        const date = new Date(result.requestTimestamp).toLocaleString();
        
        return `
            <div class="result-item ${resultClass}" onclick="viewResultDetails(${result.id})">
                <div class="d-flex align-items-center justify-content-between">
                    <div class="d-flex align-items-center">
                        <i class="fas ${statusIcon} ${statusColor} fa-lg me-3"></i>
                        <div>
                            <div class="fw-bold">${result.name} #${result.iterationNumber}</div>
                            <small class="text-muted">${date}</small>
                        </div>
                    </div>
                    <div class="text-end">
                        <div class="fw-bold">INPUT ⟹ <span class="response-time ${statusColor}">${result.responseTimeMs}ms</span> ⟹ OUTPUT</div>
                        <small class="${statusColor}">${isSuccess ? 'SUCCESS' : 'FAILED'}</small>
                    </div>
                </div>
            </div>
        `;
    }).join('');
    
    container.innerHTML = html;
}

// Add new result to live view
function addResultToLiveView(result) {
    const container = document.getElementById('recentResults');
    const isSuccess = result.isSuccessful;
    const resultClass = isSuccess ? 'result-success' : 'result-error';
    const statusIcon = isSuccess ? 'fa-check-circle' : 'fa-times-circle';
    const statusColor = isSuccess ? 'text-success' : 'text-danger';
    const date = new Date(result.requestTimestamp).toLocaleString();
    
    const resultHtml = `
        <div class="result-item ${resultClass} animate-in" onclick="viewResultDetails(${result.id})">
            <div class="d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center">
                    <i class="fas ${statusIcon} ${statusColor} fa-lg me-3"></i>
                    <div>
                        <div class="fw-bold">${result.name} #${result.iterationNumber}</div>
                        <small class="text-muted">${date}</small>
                    </div>
                </div>
                <div class="text-end">
                    <div class="fw-bold">INPUT ⟹ <span class="response-time ${statusColor}">${result.responseTimeMs}ms</span> ⟹ OUTPUT</div>
                    <small class="${statusColor}">${isSuccess ? 'SUCCESS' : 'FAILED'}</small>
                </div>
            </div>
        </div>
    `;
    
    // Add to top of results
    if (container.firstChild && !container.querySelector('.text-center')) {
        container.insertAdjacentHTML('afterbegin', resultHtml);
    } else {
        container.innerHTML = resultHtml;
    }
    
    // Limit to 10 most recent results
    const items = container.querySelectorAll('.result-item');
    if (items.length > 10) {
        items[items.length - 1].remove();
    }
    
    // Add animation
    setTimeout(() => {
        const newItem = container.querySelector('.animate-in');
        if (newItem) {
            newItem.classList.remove('animate-in');
        }
    }, 500);
}

// Update statistics counters (simplified version)
function updateStatisticsCounters() {
    // Increment total requests
    const totalElement = document.getElementById('totalRequests');
    const current = parseInt(totalElement.textContent) || 0;
    totalElement.textContent = current + 1;
}

// Show connection status
function showConnectionStatus(connected) {
    const statusElement = document.querySelector('.header-section p');
    if (statusElement) {
        const statusText = connected ? 
            'Professional API testing and monitoring dashboard • <span class="text-success"><i class="fas fa-wifi"></i> Connected</span>' :
            'Professional API testing and monitoring dashboard • <span class="text-danger"><i class="fas fa-wifi"></i> Disconnected</span>';
        statusElement.innerHTML = statusText;
    }
}

// Load sample template
function loadSampleTemplate() {
    const sampleTemplate = `{
  "model": "fraud-detector:stable",
  "messages": [
    {
      "role": "user",
      "content": "User Profile Summary:\\r\\n- {{user_profile}}\\r\\n- {{user_activity}}\\r\\n\\r\\nTransaction Context:\\r\\n- Amount Risk Score: {{amount_risk_score}}\\r\\n- Amount Z-Score: {{amount_z_score}}\\r\\n- High Amount Flag: {{high_amount_flag}}\\r\\n- New Activity Code: {{new_activity_code}}\\r\\n- New NewFrom Account: {{new_from_account}}\\r\\n- New To Account: {{new_to_account}}\\r\\n- New To City: {{new_to_city}}\\r\\n- Outside Usual Day: {{outside_usual_day}}\\r\\n\\r\\nWatchlist Indicators:\\r\\n- FromAccount: {{watchlist_from_account}}\\r\\n- FromName: {{watchlist_from_name}}\\r\\n- ToAccount: {{watchlist_to_account}}\\r\\n- ToName: {{watchlist_to_name}}\\r\\n- ToBank: {{watchlist_to_bank}}\\r\\n- IPAddress: {{watchlist_ip_address}}\\r\\n\\r\\nTransaction Details:\\r\\n- CNIC: {{random_cnic}}\\r\\n- FromAccount: {{random_account}}\\r\\n- New NewFrom Account: {{new_from_account}}\\r\\n- LogDescription: {{transaction_comments}}\\r\\n- UserId: {{user_id}}\\r\\n- FromName: {{from_name}}\\r\\n- ToAccount: {{random_iban}}\\r\\n- ToName: {{to_name}}\\r\\n- ToBank: {{to_bank}}\\r\\n- Amount: {{random_amount}}\\r\\n- DateTime: {{transaction_datetime}}\\r\\n- ActivityCode: {{activity_code}}\\r\\n- UserType: {{user_type}}\\r\\n- TransactionComments: {{transaction_comments}}"
    }
  ],
  "stream": false
}`;
    
    document.getElementById('requestTemplate').value = sampleTemplate;
    
    // Show available placeholders
    const placeholderInfo = `
    <div class="alert alert-info mt-2">
        <strong>Available Placeholders:</strong><br>
        <div class="row">
            <div class="col-md-6">
                <strong>Basic Values:</strong><br>
                <code>{{iteration}}</code> - Current iteration number<br>
                <code>{{timestamp}}</code> - Current timestamp<br>
                <code>{{iso_timestamp}}</code> - ISO formatted timestamp<br>
                <code>{{random}}</code> - Random 4-digit number<br>
                <code>{{random_amount}}</code> - Random amount<br>
                <code>{{random_cnic}}</code> - Random CNIC<br>
                <code>{{random_account}}</code> - Random account number<br>
                <code>{{random_iban}}</code> - Random IBAN<br>
                <code>{{user_id}}</code> - Random user ID<br>
                <code>{{transaction_datetime}}</code> - Random transaction datetime<br><br>
                
                <strong>Dynamic Content:</strong><br>
                <code>{{user_profile}}</code> - Random user profile<br>
                <code>{{user_activity}}</code> - Random user activity<br>
                <code>{{from_name}}</code> - Random from name<br>
                <code>{{to_name}}</code> - Random to name<br>
                <code>{{transaction_comments}}</code> - Random comments<br>
                <code>{{activity_code}}</code> - Random activity code<br>
                <code>{{user_type}}</code> - Random user type<br>
                <code>{{to_bank}}</code> - Random bank code
            </div>
            <div class="col-md-6">
                <strong>Transaction Context:</strong><br>
                <code>{{amount_risk_score}}</code> - Random amount risk score (1-10)<br>
                <code>{{amount_z_score}}</code> - Random Z-score (0-6)<br>
                <code>{{high_amount_flag}}</code> - Random Yes/No<br>
                <code>{{new_activity_code}}</code> - Random Yes/No<br>
                <code>{{new_from_account}}</code> - Random Yes/No<br>
                <code>{{new_to_account}}</code> - Random Yes/No<br>
                <code>{{new_to_city}}</code> - Random Yes/No<br>
                <code>{{outside_usual_day}}</code> - Random Yes/No<br><br>
                
                <strong>Watchlist Indicators:</strong><br>
                <code>{{watchlist_from_account}}</code> - Random Yes/No<br>
                <code>{{watchlist_from_name}}</code> - Random Yes/No<br>
                <code>{{watchlist_to_account}}</code> - Random Yes/No<br>
                <code>{{watchlist_to_name}}</code> - Random Yes/No<br>
                <code>{{watchlist_to_bank}}</code> - Random Yes/No<br>
                <code>{{watchlist_ip_address}}</code> - Random Yes/No
            </div>
        </div>
    </div>
    `;
    
    // Add info after textarea if not already there
    if (!document.querySelector('.placeholder-info')) {
        const textarea = document.getElementById('requestTemplate');
        const infoDiv = document.createElement('div');
        infoDiv.className = 'placeholder-info';
        infoDiv.innerHTML = placeholderInfo;
        textarea.parentNode.insertBefore(infoDiv, textarea.nextSibling);
    }
}

// Save configuration
async function saveConfiguration() {
    const formData = {
        id: parseInt(document.getElementById('configId').value) || 0,
        name: document.getElementById('configName').value,
        apiEndpoint: document.getElementById('apiEndpoint').value,
        requestTemplate: document.getElementById('requestTemplate').value,
        bearerToken: document.getElementById('bearerToken').value,
        delayBetweenRequests: parseInt(document.getElementById('delayBetweenRequests').value),
        maxIterations: parseInt(document.getElementById('maxIterations').value)
    };
    
    try {
        const url = formData.id ? `${API_BASE}/configuration/${formData.id}` : `${API_BASE}/configuration`;
        const method = formData.id ? 'PUT' : 'POST';
        
        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(formData)
        });
        
        if (response.ok) {
            bootstrap.Modal.getInstance(document.getElementById('configModal')).hide();
            loadConfigurations();
            resetConfigForm();
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Failed to save configuration. Please try again.',
                confirmButtonColor: '#dc3545'
            });
        }
    } catch (error) {
        console.error('Error saving configuration:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Failed to save configuration. Please try again.',
            confirmButtonColor: '#dc3545'
        });
    }
}

// Reset config form
function resetConfigForm() {
    document.getElementById('configForm').reset();
    document.getElementById('configId').value = '';
    document.getElementById('bearerToken').value = '';
}

// Edit configuration
async function editConfiguration(id) {
    try {
        const response = await fetch(`${API_BASE}/configuration/${id}`);
        const config = await response.json();
        
        document.getElementById('configId').value = config.id;
        document.getElementById('configName').value = config.name;
        document.getElementById('apiEndpoint').value = config.apiEndpoint;
        document.getElementById('requestTemplate').value = config.requestTemplate;
        document.getElementById('bearerToken').value = config.bearerToken || '';
        document.getElementById('delayBetweenRequests').value = config.delayBetweenRequests;
        document.getElementById('maxIterations').value = config.maxIterations;
        
        new bootstrap.Modal(document.getElementById('configModal')).show();
    } catch (error) {
        console.error('Error loading configuration:', error);
    }
}

// Start configuration
async function startConfiguration(id) {
    try {
        const response = await fetch(`${API_BASE}/configuration/${id}/start`, {
            method: 'POST'
        });
        
        if (response.ok) {
            loadConfigurations();
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Failed to start configuration. Please try again.',
                confirmButtonColor: '#dc3545'
            });
        }
    } catch (error) {
        console.error('Error starting configuration:', error);
    }
}

// Stop configuration
async function stopConfiguration(id) {
    try {
        const response = await fetch(`${API_BASE}/configuration/${id}/stop`, {
            method: 'POST'
        });
        
        if (response.ok) {
            loadConfigurations();
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Failed to stop configuration. Please try again.',
                confirmButtonColor: '#dc3545'
            });
        }
    } catch (error) {
        console.error('Error stopping configuration:', error);
    }
}

// Delete configuration
async function deleteConfiguration(id) {
    const result = await Swal.fire({
        icon: 'warning',
        title: 'Confirm Deletion',
        text: 'Are you sure you want to delete this configuration? This will also delete all related request logs.',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, Delete',
        cancelButtonText: 'Cancel'
    });
    
    if (!result.isConfirmed) {
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/configuration/${id}`, {
            method: 'DELETE'
        });
        
        if (response.ok) {
            loadConfigurations();
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Failed to delete configuration. Please try again.',
                confirmButtonColor: '#dc3545'
            });
        }
    } catch (error) {
        console.error('Error deleting configuration:', error);
    }
}

// View result details
async function viewResultDetails(resultId) {
    try {
        const response = await fetch(`${API_BASE}/results/${resultId}`);
        const result = await response.json();
        
        const details = `
            <div class="row">
                <div class="col-md-6">
                    <h6>Request Information</h6>
                    <p><strong>Configuration:</strong> ${result.apiConfiguration.name}</p>
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
                        <pre>${JSON.stringify(JSON.parse(result.requestPayload), null, 2)}</pre>
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
    }
}

// View results for configuration
function viewResults(configId) {
    window.open(`/Results?configId=${configId}`, '_blank');
}

// Clear logs for specific configuration
async function clearConfigurationLogs(configId) {
    const config = configurations.find(c => c.id === configId);
    const configName = config ? config.name : 'this configuration';
    
    const result = await Swal.fire({
        icon: 'warning',
        title: 'Clear Logs',
        text: `Are you sure you want to clear all logs for "${configName}"? This action cannot be undone.`,
        showCancelButton: true,
        confirmButtonColor: '#ffc107',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, Clear Logs',
        cancelButtonText: 'Cancel'
    });
    
    if (!result.isConfirmed) {
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/results/configuration/${configId}`, {
            method: 'DELETE'
        });
        
        if (response.ok) {
            const result = await response.json();
            Swal.fire({
                icon: 'success',
                title: 'Success',
                text: result.message,
                confirmButtonColor: '#198754'
            });
            loadData(); // Reload dashboard data
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Failed to clear logs. Please try again.',
                confirmButtonColor: '#dc3545'
            });
        }
    } catch (error) {
        console.error('Error clearing configuration logs:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Failed to clear logs. Please try again.',
            confirmButtonColor: '#dc3545'
        });
    }
}

// Clear all logs
async function clearAllLogs() {
    const result = await Swal.fire({
        icon: 'warning',
        title: 'Clear All Logs',
        text: 'Are you sure you want to clear ALL logs for ALL configurations? This action cannot be undone.',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, Clear All',
        cancelButtonText: 'Cancel'
    });
    
    if (!result.isConfirmed) {
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/results`, {
            method: 'DELETE'
        });
        
        if (response.ok) {
            const result = await response.json();
            Swal.fire({
                icon: 'success',
                title: 'Success',
                text: result.message,
                confirmButtonColor: '#198754'
            });
            loadData(); // Reload dashboard data
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'Failed to clear all logs. Please try again.',
                confirmButtonColor: '#dc3545'
            });
        }
    } catch (error) {
        console.error('Error clearing all logs:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'Failed to clear all logs. Please try again.',
            confirmButtonColor: '#dc3545'
        });
    }
}


// Clear any existing modal backdrops
function clearModalBackdrops() {
    // Remove any existing modal backdrops that might be stuck
    const backdrops = document.querySelectorAll('.modal-backdrop');
    backdrops.forEach(backdrop => backdrop.remove());
    
    // Remove modal-open class from body if it exists
    document.body.classList.remove('modal-open');
    
    // Reset body padding that might be added by Bootstrap
    document.body.style.paddingRight = '';
    document.body.style.overflow = '';
    
    // Hide any open modals
    const openModals = document.querySelectorAll('.modal.show');
    openModals.forEach(modal => {
        const modalInstance = bootstrap.Modal.getInstance(modal);
        if (modalInstance) {
            modalInstance.hide();
        } else {
            modal.classList.remove('show');
            modal.style.display = 'none';
        }
    });
}

// Emergency modal cleanup - can be called from browser console if needed
function emergencyModalCleanup() {
    console.log('Running emergency modal cleanup...');
    
    // Force remove all modal backdrops
    document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
    
    // Force hide all modals
    document.querySelectorAll('.modal').forEach(modal => {
        modal.classList.remove('show');
        modal.style.display = 'none';
        modal.setAttribute('aria-hidden', 'true');
        modal.removeAttribute('aria-modal');
        modal.removeAttribute('role');
    });
    
    // Reset body state
    document.body.classList.remove('modal-open');
    document.body.style.paddingRight = '';
    document.body.style.overflow = '';
    
    console.log('Emergency cleanup completed!');
}

// Make emergency cleanup available globally
window.emergencyModalCleanup = emergencyModalCleanup;

// Initialize event listeners
function initializeEventListeners() {
    // Start All button
    const startAllBtn = document.getElementById('startAllBtn');
    if (startAllBtn) {
        startAllBtn.addEventListener('click', async () => {
            try {
                const response = await fetch(`${API_BASE}/configuration/start-all`, {
                    method: 'POST'
                });
                if (response.ok) {
                    loadData();
                } else {
                    console.error('Failed to start all configurations');
                }
            } catch (error) {
                console.error('Error starting all configurations:', error);
            }
        });
    }
    
    // Stop All button
    const stopAllBtn = document.getElementById('stopAllBtn');
    if (stopAllBtn) {
        stopAllBtn.addEventListener('click', async () => {
            try {
                const response = await fetch(`${API_BASE}/configuration/stop-all`, {
                    method: 'POST'
                });
                if (response.ok) {
                    loadData();
                } else {
                    console.error('Failed to stop all configurations');
                }
            } catch (error) {
                console.error('Error stopping all configurations:', error);
            }
        });
    }
    
    // Refresh button
    const refreshBtn = document.getElementById('refreshBtn');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', loadData);
    }
    
    // Add Config button
    const addConfigBtn = document.getElementById('addConfigBtn');
    if (addConfigBtn) {
        addConfigBtn.addEventListener('click', resetConfigForm);
    }
    
    // View Results button
    const viewResultsBtn = document.getElementById('viewResultsBtn');
    if (viewResultsBtn) {
        viewResultsBtn.addEventListener('click', async () => {
            if (configurations.length === 0) {
                Swal.fire({
                    icon: 'info',
                    title: 'No Configurations',
                    text: 'Please add a configuration first before viewing results.',
                    confirmButtonColor: '#0d6efd'
                });
                return;
            }
            
            if (configurations.length === 1) {
                // Open results for the only configuration
                window.open(`/Results?configId=${configurations[0].id}`, '_blank');
            } else {
                // Show selection modal or open first one
                const firstConfig = configurations[0];
                const result = await Swal.fire({
                    icon: 'question',
                    title: 'Select Configuration',
                    text: `Open results for "${firstConfig.name}"? (You can also use the chart button on individual configurations)`,
                    showCancelButton: true,
                    confirmButtonColor: '#0d6efd',
                    cancelButtonColor: '#6c757d',
                    confirmButtonText: 'Open Results',
                    cancelButtonText: 'Cancel'
                });
                
                if (result.isConfirmed) {
                    window.open(`/Results?configId=${firstConfig.id}`, '_blank');
                }
            }
        });
    }
    
    // Clear All Logs button
    const clearAllLogsBtn = document.getElementById('clearAllLogsBtn');
    if (clearAllLogsBtn) {
        clearAllLogsBtn.addEventListener('click', clearAllLogs);
    }
    
    // Reset form when modal is hidden
    const configModal = document.getElementById('configModal');
    if (configModal) {
        configModal.addEventListener('hidden.bs.modal', function() {
            resetConfigForm();
            // Clear any placeholder info that might have been added
            const placeholderInfo = document.querySelector('.placeholder-info');
            if (placeholderInfo) {
                placeholderInfo.remove();
            }
        });
        
        // Ensure modal backdrops are properly cleaned up
        configModal.addEventListener('hide.bs.modal', function() {
            setTimeout(clearModalBackdrops, 300);
        });
    }
    
    // Result modal cleanup
    const resultModal = document.getElementById('resultModal');
    if (resultModal) {
        resultModal.addEventListener('hide.bs.modal', function() {
            setTimeout(clearModalBackdrops, 300);
        });
    }
}
