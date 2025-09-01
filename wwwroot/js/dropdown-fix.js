// Dropdown fix script - directly fix dropdown issues
console.log('Dropdown fix script loaded');

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, starting dropdown fix...');
    
    // Wait a bit to ensure Bootstrap is fully loaded
    setTimeout(function() {
        fixDropdowns();
    }, 500);
});

function fixDropdowns() {
    console.log('=== DROPDOWN FIX DIAGNOSTICS ===');
    
    // Check Bootstrap availability
    console.log('Bootstrap available:', typeof bootstrap !== 'undefined');
    console.log('jQuery available:', typeof $ !== 'undefined');
    
    if (typeof bootstrap === 'undefined') {
        console.error('Bootstrap is not loaded!');
        return;
    }
    
    // Find all dropdown toggles
    const dropdowns = document.querySelectorAll('[data-bs-toggle="dropdown"]');
    console.log('Found dropdown toggles:', dropdowns.length);
    
    dropdowns.forEach((dropdown, index) => {
        console.log(`Processing dropdown ${index + 1}:`, dropdown.textContent.trim());
        
        // Check if Bootstrap instance exists
        let instance = bootstrap.Dropdown.getInstance(dropdown);
        console.log(`  - Existing instance: ${instance ? 'Yes' : 'No'}`);
        
        // Create new instance if needed
        if (!instance) {
            try {
                instance = new bootstrap.Dropdown(dropdown);
                console.log('  - Created new instance: Yes');
            } catch (error) {
                console.error('  - Error creating instance:', error);
                return;
            }
        }
        
        // Add manual click handler as backup
        dropdown.addEventListener('click', function(e) {
            console.log(`Dropdown ${index + 1} clicked`);
            
            // Let Bootstrap handle it first
            setTimeout(() => {
                const menu = dropdown.nextElementSibling;
                if (menu && menu.classList.contains('dropdown-menu')) {
                    console.log(`  - Menu visibility: ${menu.classList.contains('show') ? 'Visible' : 'Hidden'}`);
                    
                    // If Bootstrap didn't show it, show it manually
                    if (!menu.classList.contains('show')) {
                        console.log('  - Bootstrap failed, showing manually');
                        
                        // Hide all other dropdowns first
                        document.querySelectorAll('.dropdown-menu.show').forEach(otherMenu => {
                            otherMenu.classList.remove('show');
                        });
                        document.querySelectorAll('[data-bs-toggle="dropdown"][aria-expanded="true"]').forEach(otherToggle => {
                            otherToggle.setAttribute('aria-expanded', 'false');
                        });
                        
                        // Show this dropdown
                        menu.classList.add('show');
                        dropdown.setAttribute('aria-expanded', 'true');
                    }
                }
            }, 10);
        });
        
        console.log(`  - Event handler added: Yes`);
    });
    
    // Add global click handler to close dropdowns
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.dropdown')) {
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                menu.classList.remove('show');
            });
            document.querySelectorAll('[data-bs-toggle="dropdown"]').forEach(toggle => {
                toggle.setAttribute('aria-expanded', 'false');
            });
        }
    });
    
    console.log('=== DROPDOWN FIX COMPLETE ===');
}

// Manual test functions
window.testFirstDropdown = function() {
    const firstDropdown = document.querySelector('[data-bs-toggle="dropdown"]');
    if (firstDropdown) {
        console.log('Testing first dropdown manually...');
        firstDropdown.click();
    } else {
        console.log('No dropdown found to test');
    }
};

window.forceShowFirstDropdown = function() {
    const firstDropdown = document.querySelector('[data-bs-toggle="dropdown"]');
    const firstMenu = document.querySelector('.dropdown-menu');
    
    if (firstDropdown && firstMenu) {
        console.log('Force showing first dropdown...');
        firstMenu.classList.add('show');
        firstDropdown.setAttribute('aria-expanded', 'true');
    } else {
        console.log('Dropdown elements not found');
    }
};

window.debugBootstrapDropdown = function() {
    const dropdown = document.querySelector('[data-bs-toggle="dropdown"]');
    if (dropdown && typeof bootstrap !== 'undefined') {
        try {
            const instance = bootstrap.Dropdown.getOrCreateInstance(dropdown);
            console.log('Bootstrap dropdown instance:', instance);
            instance.show();
            console.log('Bootstrap show() called');
        } catch (error) {
            console.error('Error with Bootstrap dropdown:', error);
        }
    }
};
