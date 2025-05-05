// Teacher Dashboard Scripts

// Initial setup
document.addEventListener('DOMContentLoaded', function () {
    console.log('Teacher Dashboard JS Loaded');

    // Initialize all tooltips
    initTooltips();

    // Set default toastr options
    configureToastr();

    // Handle sidebar toggling on mobile
    setupSidebar();
});

// Initialize Bootstrap tooltips
function initTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl, {
            boundary: document.body
        });
    });
}

// Configure Toastr notifications
function configureToastr() {
    toastr.options = {
        "closeButton": true,
        "debug": false,
        "newestOnTop": true,
        "progressBar": true,
        "positionClass": "toast-top-right",
        "preventDuplicates": false,
        "onclick": null,
        "showDuration": "300",
        "hideDuration": "1000",
        "timeOut": "5000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };

    // Welcome message on first load (using sessionStorage)
    if (!sessionStorage.getItem('dashboardLoaded')) {
        setTimeout(function () {
            toastr.info('Welcome to the Teacher Dashboard', 'Hello!');
            sessionStorage.setItem('dashboardLoaded', 'true');
        }, 1000);
    }
}

// Handle sidebar toggling on mobile
function setupSidebar() {
    const sidebarToggle = document.body.querySelector('#sidebarToggle');
    if (sidebarToggle) {
        // Restore previous state
        if (localStorage.getItem('sb|sidebar-toggle') === 'true') {
            document.body.classList.toggle('sb-sidenav-toggled');
        }

        sidebarToggle.addEventListener('click', event => {
            event.preventDefault();
            document.body.classList.toggle('sb-sidenav-toggled');
            localStorage.setItem('sb|sidebar-toggle', document.body.classList.contains('sb-sidenav-toggled'));
        });
    }
}

// Confirmation dialog helper
function confirmAction(title, message, confirmCallback, options = {}) {
    const defaultOptions = {
        icon: 'warning',
        confirmButtonText: 'Yes, proceed',
        cancelButtonText: 'Cancel',
        confirmButtonColor: '#0d6efd',
        cancelButtonColor: '#6c757d',
        showCancelButton: true
    };

    const finalOptions = { ...defaultOptions, ...options };

    Swal.fire({
        title: title,
        text: message,
        icon: finalOptions.icon,
        showCancelButton: finalOptions.showCancelButton,
        confirmButtonColor: finalOptions.confirmButtonColor,
        cancelButtonColor: finalOptions.cancelButtonColor,
        confirmButtonText: finalOptions.confirmButtonText,
        cancelButtonText: finalOptions.cancelButtonText
    }).then((result) => {
        if (result.isConfirmed && typeof confirmCallback === 'function') {
            confirmCallback();
        }
    });
}

// Add a notification counter update function
function updateNotificationCount(count) {
    const notificationBadge = document.querySelector('#navbarDropdownNotifications .badge-counter');
    if (notificationBadge) {
        notificationBadge.textContent = count > 0 ? (count > 9 ? '9+' : count) : '';
        notificationBadge.style.display = count > 0 ? 'inline-block' : 'none';
    }
}

// Form validation helper
function validateForm(formId, options = {}) {
    const form = document.getElementById(formId);
    if (!form) return false;

    let isValid = true;
    const inputs = form.querySelectorAll('input, select, textarea');

    inputs.forEach(input => {
        if (input.hasAttribute('required') && !input.value.trim()) {
            input.classList.add('is-invalid');
            isValid = false;
        } else {
            input.classList.remove('is-invalid');

            // Add valid class if option is enabled
            if (options.showValid) {
                input.classList.add('is-valid');
            }
        }

        // Email validation
        if (input.type === 'email' && input.value.trim()) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(input.value)) {
                input.classList.add('is-invalid');
                isValid = false;
            }
        }

        // Number validation
        if (input.type === 'number' && input.value.trim()) {
            const min = parseFloat(input.getAttribute('min'));
            const max = parseFloat(input.getAttribute('max'));
            const value = parseFloat(input.value);

            if ((min !== null && value < min) || (max !== null && value > max)) {
                input.classList.add('is-invalid');
                isValid = false;
            }
        }
    });

    return isValid;
}

// Format date helper
function formatDate(date, format = 'short') {
    if (!date) return '';

    const d = new Date(date);

    if (format === 'short') {
        return d.toLocaleDateString();
    } else if (format === 'long') {
        return d.toLocaleDateString(undefined, {
            weekday: 'long',
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    } else if (format === 'time') {
        return d.toLocaleTimeString();
    } else if (format === 'datetime') {
        return d.toLocaleString();
    }

    return d.toDateString();
}

// Format currency helper
function formatCurrency(amount, currency = 'VND') {
    if (typeof amount !== 'number') {
        amount = parseFloat(amount) || 0;
    }

    if (currency === 'VND') {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
            maximumFractionDigits: 0
        }).format(amount);
    }

    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency
    }).format(amount);
}

// Add a guided tour helper
function startGuidedTour(steps) {
    if (!steps || !steps.length) {
        console.error('No steps provided for the guided tour');
        return;
    }

    introJs().setOptions({
        steps: steps,
        showStepNumbers: false,
        exitOnOverlayClick: false,
        scrollToElement: true,
        showBullets: true,
        showProgress: true,
        disableInteraction: false
    }).start();
}

// AJAX request helper
function makeAjaxRequest(url, method, data, successCallback, errorCallback) {
    const xhr = new XMLHttpRequest();

    xhr.open(method, url, true);
    xhr.setRequestHeader('Content-Type', 'application/json');
    xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');

    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4) {
            if (xhr.status >= 200 && xhr.status < 300) {
                let response;
                try {
                    response = JSON.parse(xhr.responseText);
                } catch (e) {
                    response = xhr.responseText;
                }

                if (typeof successCallback === 'function') {
                    successCallback(response);
                }
            } else {
                if (typeof errorCallback === 'function') {
                    let errorResponse;
                    try {
                        errorResponse = JSON.parse(xhr.responseText);
                    } catch (e) {
                        errorResponse = { message: 'An error occurred' };
                    }

                    errorCallback(errorResponse, xhr.status);
                }
            }
        }
    };

    xhr.onerror = function () {
        if (typeof errorCallback === 'function') {
            errorCallback({ message: 'Network error occurred' });
        }
    };

    if (data) {
        xhr.send(JSON.stringify(data));
    } else {
        xhr.send();
    }
}