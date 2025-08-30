// Global JavaScript functionality for Fraud Detector Pro

// Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', function() {
    // Force Bootstrap initialization
    setTimeout(initializeAllBootstrapComponents, 100);
});

// Force initialize all Bootstrap components
function initializeAllBootstrapComponents() {
    try {
        // Initialize dropdowns
        const dropdownTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="dropdown"]'));
        const dropdownList = dropdownTriggerList.map(function (dropdownTriggerEl) {
            return new bootstrap.Dropdown(dropdownTriggerEl);
        });
        
        // Initialize modals
        const modalList = [].slice.call(document.querySelectorAll('.modal'));
        modalList.forEach(function(modalEl) {
            new bootstrap.Modal(modalEl);
        });
        
        // Initialize tooltips
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.forEach(function(tooltipTriggerEl) {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });
        
        // Initialize popovers
        const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggerList.forEach(function(popoverTriggerEl) {
            new bootstrap.Popover(popoverTriggerEl);
        });
        
        console.log('Bootstrap components initialized successfully');
    } catch (error) {
        console.error('Error initializing Bootstrap components:', error);
        
        // Fallback: Add click handlers manually for dropdowns
        addManualDropdownHandlers();
    }
}

// Manual dropdown handlers as fallback
function addManualDropdownHandlers() {
    const dropdownButtons = document.querySelectorAll('[data-bs-toggle="dropdown"]');
    dropdownButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            // Close all other dropdowns
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                menu.classList.remove('show');
            });
            
            // Toggle current dropdown
            const menu = this.nextElementSibling;
            if (menu && menu.classList.contains('dropdown-menu')) {
                menu.classList.toggle('show');
                this.setAttribute('aria-expanded', menu.classList.contains('show'));
            }
        });
    });
    
    // Close dropdowns when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.dropdown')) {
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                menu.classList.remove('show');
            });
            document.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(button => {
                button.setAttribute('aria-expanded', 'false');
            });
        }
    });
    
    console.log('Manual dropdown handlers added');
}

// Global toast function
function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    toast.className = `alert alert-${type === 'success' ? 'success' : 'danger'} position-fixed alert-dismissible fade show`;
    toast.style.cssText = 'top: 80px; right: 20px; z-index: 9999; min-width: 250px;';
    toast.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check' : 'exclamation-triangle'} me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
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
