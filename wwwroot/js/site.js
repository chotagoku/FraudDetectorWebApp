// Global JavaScript functionality for Fraud Detector Pro

// Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('site.js DOMContentLoaded fired');
    // Initialize Bootstrap components once
    initializeAllBootstrapComponents();
});

// Force initialize all Bootstrap components
function initializeAllBootstrapComponents() {
    try {
        console.log('Bootstrap available:', typeof bootstrap !== 'undefined');
        
        if (typeof bootstrap !== 'undefined') {
            // Initialize dropdowns - let Bootstrap handle them natively
            const dropdownTriggerList = document.querySelectorAll('[data-bs-toggle="dropdown"]');
            console.log('Found dropdown elements:', dropdownTriggerList.length);
            
            // Just ensure they're properly initialized by Bootstrap itself
            dropdownTriggerList.forEach(function (dropdownTriggerEl) {
                // Only create if not already initialized
                if (!bootstrap.Dropdown.getInstance(dropdownTriggerEl)) {
                    new bootstrap.Dropdown(dropdownTriggerEl);
                }
            });
            
            // Initialize modals
            const modalList = document.querySelectorAll('.modal');
            modalList.forEach(function(modalEl) {
                if (!bootstrap.Modal.getInstance(modalEl)) {
                    new bootstrap.Modal(modalEl);
                }
            });
            
            console.log('Bootstrap components initialized successfully');
        } else {
            console.error('Bootstrap is not available');
        }
    } catch (error) {
        console.error('Error initializing Bootstrap components:', error);
    }
}


// Global toast function
function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    const alertType = type === 'success' ? 'success' : 'danger';
    toast.className = 'alert alert-' + alertType + ' position-fixed alert-dismissible fade show';
    toast.style.cssText = 'top: 80px; right: 20px; z-index: 9999; min-width: 250px;';
    
    const iconClass = type === 'success' ? 'check' : 'exclamation-triangle';
    toast.innerHTML = '<i class="fas fa-' + iconClass + ' me-2"></i>' + message + 
        '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>';
    
    document.body.appendChild(toast);
    
    setTimeout(() => {
        if (toast.parentNode) {
            toast.remove();
        }
    }, 5000);
}

// Global loading state function
function setGlobalLoadingState(loading, elementId = null) {
    const element = elementId ? document.getElementById(elementId) : document.body;
    if (loading) {
        element.style.cursor = 'wait';
    } else {
        element.style.cursor = '';
    }
}

// Utility function to format numbers
function formatNumber(num) {
    return new Intl.NumberFormat().format(num);
}

// Utility function to format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-PK', {
        style: 'currency',
        currency: 'PKR',
        minimumFractionDigits: 0
    }).format(amount);
}

// Debug function to check Bootstrap
function checkBootstrap() {
    console.log('Bootstrap available:', typeof bootstrap !== 'undefined');
    console.log('jQuery available:', typeof $ !== 'undefined');
    if (typeof bootstrap !== 'undefined') {
        console.log('Bootstrap components:', Object.keys(bootstrap));
    }
}
