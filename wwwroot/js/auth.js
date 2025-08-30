// Authentication Pages JavaScript

// Toggle password visibility
function togglePassword(inputId) {
    const passwordInput = document.getElementById(inputId || 'password');
    const toggleIcon = document.getElementById((inputId || 'password') + 'ToggleIcon');
    
    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        toggleIcon.classList.remove('fa-eye');
        toggleIcon.classList.add('fa-eye-slash');
    } else {
        passwordInput.type = 'password';
        toggleIcon.classList.remove('fa-eye-slash');
        toggleIcon.classList.add('fa-eye');
    }
}

// Check password strength (for register page)
function checkPasswordStrength(password) {
    let score = 0;
    const requirements = {
        length: password.length >= 8,
        uppercase: /[A-Z]/.test(password),
        lowercase: /[a-z]/.test(password),
        number: /\d/.test(password),
        special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
    };

    // Update requirements list
    for (const [key, isValid] of Object.entries(requirements)) {
        const element = document.getElementById(`req-${key}`);
        if (element) {
            element.className = isValid ? 'valid' : '';
        }
        if (isValid) score++;
    }

    const strengthElement = document.getElementById('passwordStrength');
    if (!strengthElement) return; // Only for register page
    
    if (password.length === 0) {
        strengthElement.innerHTML = '';
        return;
    }

    let strengthText = '';
    let strengthClass = '';

    if (score < 3) {
        strengthText = 'Weak password';
        strengthClass = 'strength-weak';
    } else if (score < 5) {
        strengthText = 'Medium strength password';
        strengthClass = 'strength-medium';
    } else {
        strengthText = 'Strong password';
        strengthClass = 'strength-strong';
    }

    strengthElement.innerHTML = `<i class="fas fa-info-circle me-1"></i>${strengthText}`;
    strengthElement.className = `password-strength ${strengthClass}`;
}

// Initialize floating labels
function initializeFloatingLabels() {
    document.querySelectorAll('.form-floating input').forEach(input => {
        // Handle initial state
        checkLabelPosition(input);
        
        // Add event listeners
        input.addEventListener('input', function() {
            checkLabelPosition(this);
        });
        
        input.addEventListener('focus', function() {
            this.parentElement.classList.add('focused');
        });
        
        input.addEventListener('blur', function() {
            this.parentElement.classList.remove('focused');
            checkLabelPosition(this);
        });
    });
}

// Check if label should be moved up
function checkLabelPosition(input) {
    if (input.value && input.value.trim() !== '') {
        input.classList.add('has-value');
    } else {
        input.classList.remove('has-value');
    }
}

// Initialize event listeners when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Initialize floating labels
    initializeFloatingLabels();
    
    // Password strength checking for register page
    const passwordInput = document.getElementById('password');
    if (passwordInput) {
        passwordInput.addEventListener('input', function() {
            checkPasswordStrength(this.value);
        });
    }

    // Register form submission
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const formData = {
                firstName: document.getElementById('firstName').value,
                lastName: document.getElementById('lastName').value,
                email: document.getElementById('email').value,
                phone: document.getElementById('phone').value,
                company: document.getElementById('company').value,
                password: document.getElementById('password').value,
                confirmPassword: document.getElementById('confirmPassword').value
            };

            const submitBtn = document.querySelector('.btn-register');
            const buttonText = document.querySelector('.button-text');
            const loadingSpinner = document.querySelector('.loading-spinner');

            // Validation
            if (!formData.firstName || !formData.lastName || !formData.email || !formData.password || !formData.confirmPassword) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Missing Information',
                    text: 'Please fill in all required fields.',
                    confirmButtonColor: '#667eea'
                });
                return;
            }

            if (formData.password !== formData.confirmPassword) {
                Swal.fire({
                    icon: 'error',
                    title: 'Password Mismatch',
                    text: 'Passwords do not match. Please try again.',
                    confirmButtonColor: '#dc3545'
                });
                return;
            }

            if (formData.password.length < 8) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Weak Password',
                    text: 'Password must be at least 8 characters long.',
                    confirmButtonColor: '#ffc107'
                });
                return;
            }

            if (!document.getElementById('agreeTerms').checked) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Terms Agreement Required',
                    text: 'Please accept the Terms of Service and Privacy Policy.',
                    confirmButtonColor: '#667eea'
                });
                return;
            }

            // Show loading state
            submitBtn.disabled = true;
            buttonText.textContent = 'Creating Account...';
            loadingSpinner.style.display = 'inline-block';

            try {
                // Make actual API call to register endpoint
                const response = await fetch('/api/account/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(formData)
                });
                
                const result = await response.json();
                
                if (response.ok) {
                    const message = result.message || result.data?.message || 'Your account has been created successfully.';
                    Swal.fire({
                        icon: 'success',
                        title: 'Account Created!',
                        text: message,
                        confirmButtonColor: '#198754'
                    }).then(() => {
                        // Redirect to login page
                        window.location.href = '/Account/Login';
                    });
                } else {
                    throw new Error(result.message || result.error || 'Registration failed');
                }
            } catch (error) {
                console.error('Registration error:', error);
                Swal.fire({
                    icon: 'error',
                    title: 'Registration Failed',
                    text: error.message || 'There was an error creating your account. Please try again.',
                    confirmButtonColor: '#dc3545'
                });
            } finally {
                // Reset button state
                submitBtn.disabled = false;
                buttonText.textContent = 'Create Account';
                loadingSpinner.style.display = 'none';
            }
        });
    }

    // Login form submission
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const formData = {
                email: document.getElementById('email').value,
                password: document.getElementById('password').value,
                rememberMe: document.getElementById('rememberMe').checked
            };

            const submitBtn = document.querySelector('.btn-login');
            const buttonText = document.querySelector('.button-text');
            const loadingSpinner = document.querySelector('.loading-spinner');

            // Validation
            if (!formData.email || !formData.password) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Missing Information',
                    text: 'Please enter both email and password.',
                    confirmButtonColor: '#667eea'
                });
                return;
            }

            // Show loading state
            submitBtn.disabled = true;
            buttonText.textContent = 'Signing In...';
            loadingSpinner.style.display = 'inline-block';

            try {
                // Make actual API call to login endpoint
                const response = await fetch('/api/account/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        email: formData.email,
                        password: formData.password
                    })
                });
                
                const result = await response.json();
                
                if (response.ok) {
                    const message = result.message || result.data?.message || 'You have been logged in successfully.';
                    const userName = result.data?.name || result.user?.name || 'Welcome back';
                    
                    Swal.fire({
                        icon: 'success',
                        title: `Welcome Back, ${userName}!`,
                        text: message,
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        // Redirect to dashboard (the main page)
                        window.location.href = '/';
                    });
                } else {
                    throw new Error(result.message || result.error || 'Invalid credentials');
                }
            } catch (error) {
                console.error('Login error:', error);
                Swal.fire({
                    icon: 'error',
                    title: 'Login Failed',
                    text: error.message || 'Invalid email or password. Please try again.',
                    confirmButtonColor: '#dc3545'
                });
            } finally {
                // Reset button state
                submitBtn.disabled = false;
                buttonText.textContent = 'Sign In';
                loadingSpinner.style.display = 'none';
            }
        });
    }
});
