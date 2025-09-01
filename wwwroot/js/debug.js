// Debug script to test dropdown functionality
console.log('=== DROPDOWN DEBUG SCRIPT LOADED ===');

// Wait for page to load
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM Content Loaded - Starting dropdown debug');
    
    // Check if Bootstrap is loaded
    console.log('Bootstrap available:', typeof bootstrap !== 'undefined');
    if (typeof bootstrap !== 'undefined') {
        console.log('Bootstrap components:', Object.keys(bootstrap));
    }
    
    // Check if jQuery is loaded
    console.log('jQuery available:', typeof $ !== 'undefined');
    
    // Find all dropdown toggles
    const dropdownToggles = document.querySelectorAll('[data-bs-toggle="dropdown"]');
    console.log('Found dropdown toggles:', dropdownToggles.length);
    
    dropdownToggles.forEach((toggle, index) => {
        console.log(`Dropdown ${index + 1}:`, toggle);
        console.log(`  - ID: ${toggle.id}`);
        console.log(`  - Classes: ${toggle.className}`);
        console.log(`  - Text: ${toggle.textContent.trim()}`);
        
        // Try to initialize Bootstrap dropdown
        try {
            if (typeof bootstrap !== 'undefined' && bootstrap.Dropdown) {
                const dropdown = new bootstrap.Dropdown(toggle);
                console.log(`  - Bootstrap dropdown initialized successfully`);
            }
        } catch (error) {
            console.error(`  - Bootstrap dropdown initialization failed:`, error);
        }
        
        // Add manual click handler for debugging
        toggle.addEventListener('click', function(e) {
            console.log(`Dropdown ${index + 1} clicked!`);
            console.log('Event:', e);
            console.log('Target:', e.target);
            console.log('Current target:', e.currentTarget);
            
            // Prevent default to see if that helps
            e.preventDefault();
            e.stopPropagation();
            
            // Try to find and toggle the dropdown menu
            const menu = this.nextElementSibling;
            if (menu && menu.classList.contains('dropdown-menu')) {
                console.log('Found dropdown menu, toggling...');
                menu.classList.toggle('show');
                this.setAttribute('aria-expanded', menu.classList.contains('show'));
            } else {
                console.log('No dropdown menu found next to toggle');
                console.log('Next element:', menu);
            }
        });
    });
    
    // Check for dropdown menus
    const dropdownMenus = document.querySelectorAll('.dropdown-menu');
    console.log('Found dropdown menus:', dropdownMenus.length);
    
    // Add click outside handler
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.dropdown')) {
            console.log('Clicked outside dropdown, closing all');
            dropdownMenus.forEach(menu => {
                menu.classList.remove('show');
            });
            dropdownToggles.forEach(toggle => {
                toggle.setAttribute('aria-expanded', 'false');
            });
        }
    });
    
    console.log('=== DROPDOWN DEBUG SETUP COMPLETE ===');
});

// Function to manually test dropdown
window.testDropdown = function() {
    console.log('=== MANUAL DROPDOWN TEST ===');
    const toggles = document.querySelectorAll('[data-bs-toggle="dropdown"]');
    if (toggles.length > 0) {
        const firstToggle = toggles[0];
        console.log('Testing first dropdown:', firstToggle);
        firstToggle.click();
    } else {
        console.log('No dropdown toggles found');
    }
};

// Function to check Bootstrap status
window.checkBootstrap = function() {
    console.log('=== BOOTSTRAP STATUS CHECK ===');
    console.log('Bootstrap object:', typeof bootstrap !== 'undefined' ? bootstrap : 'undefined');
    if (typeof bootstrap !== 'undefined') {
        console.log('Bootstrap.Dropdown:', bootstrap.Dropdown);
        
        // Try to create a dropdown instance
        const toggle = document.querySelector('[data-bs-toggle="dropdown"]');
        if (toggle) {
            try {
                const dropdown = new bootstrap.Dropdown(toggle);
                console.log('Bootstrap dropdown instance created:', dropdown);
            } catch (error) {
                console.error('Failed to create Bootstrap dropdown:', error);
            }
        }
    }
};
