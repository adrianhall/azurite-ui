/**
 * Toast notification utility using Bootstrap 5 Toast component
 * Provides success, error, warning, and info notifications
 *
 * @example
 * // Basic usage with auto-dismiss (5 seconds for success, 10 seconds for errors)
 * Toast.success('Operation completed successfully');
 * Toast.error('Something went wrong');
 * Toast.warning('Please check your input');
 * Toast.info('Here is some information');
 *
 * @example
 * // Custom duration (in milliseconds)
 * Toast.success('Quick message', 3000);  // Dismiss after 3 seconds
 * Toast.error('Important error', 15000); // Dismiss after 15 seconds
 *
 * @example
 * // Sticky toasts that require manual dismissal
 * Toast.error('Critical error - please review', null, true);
 * Toast.warning('Action required', null, true);
 *
 * @example
 * // Using the base showToast function directly
 * showToast('Custom message', 'info', 5000, false);
 * showToast('Sticky custom message', 'warning', null, true);
 */

/**
 * Shows a toast notification
 * @param {string} message - The message to display
 * @param {string} type - The type of toast: 'success', 'error', 'warning', or 'info'
 * @param {number} duration - Duration in milliseconds (default: 5000 for success, 10000 for errors)
 * @param {boolean} isSticky - If true, toast will not auto-dismiss and requires manual dismissal (default: false)
 */
function showToast(message, type = 'info', duration = null, isSticky = false) {
    // Default durations based on type (only used if not sticky)
    if (duration === null && !isSticky) {
        duration = type === 'error' ? 10000 : 5000;
    }

    // Get or create toast container
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        container.setAttribute('data-testid', 'toast-container');
        document.body.appendChild(container);
    }

    // Define toast styling based on type
    const typeConfig = {
        success: {
            bgClass: 'bg-success',
            icon: 'bi-check-circle-fill',
            title: 'Success'
        },
        error: {
            bgClass: 'bg-danger',
            icon: 'bi-exclamation-circle-fill',
            title: 'Error'
        },
        warning: {
            bgClass: 'bg-warning',
            icon: 'bi-exclamation-triangle-fill',
            title: 'Warning'
        },
        info: {
            bgClass: 'bg-info',
            icon: 'bi-info-circle-fill',
            title: 'Info'
        }
    };

    const config = typeConfig[type] || typeConfig.info;

    // Create unique ID for this toast
    const toastId = `toast-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

    // Create toast element
    const toastEl = document.createElement('div');
    toastEl.className = 'toast';
    toastEl.id = toastId;
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-live', 'assertive');
    toastEl.setAttribute('aria-atomic', 'true');
    toastEl.setAttribute('data-testid', 'toast');
    toastEl.setAttribute('data-toast-type', type);

    // Build toast HTML
    toastEl.innerHTML = `
        <div class="toast-header ${config.bgClass} text-white">
            <i class="bi ${config.icon} me-2"></i>
            <strong class="me-auto">${config.title}</strong>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body">
            ${escapeHtml(message)}
        </div>
    `;

    // Add to container
    container.appendChild(toastEl);

    // Initialize Bootstrap toast with sticky or auto-dismiss behavior
    const toastOptions = {
        autohide: !isSticky
    };

    // Only set delay if not sticky
    if (!isSticky && duration) {
        toastOptions.delay = duration;
    }

    const bsToast = new bootstrap.Toast(toastEl, toastOptions);

    // Remove from DOM after hidden
    toastEl.addEventListener('hidden.bs.toast', () => {
        toastEl.remove();
    });

    // Show the toast
    bsToast.show();
}

/**
 * Escapes HTML to prevent XSS attacks
 * @param {string} text - The text to escape
 * @returns {string} Escaped text
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Convenience methods for specific toast types
 */
const Toast = {
    success: (message, duration, isSticky) => showToast(message, 'success', duration, isSticky),
    error: (message, duration, isSticky) => showToast(message, 'error', duration, isSticky),
    warning: (message, duration, isSticky) => showToast(message, 'warning', duration, isSticky),
    info: (message, duration, isSticky) => showToast(message, 'info', duration, isSticky)
};
