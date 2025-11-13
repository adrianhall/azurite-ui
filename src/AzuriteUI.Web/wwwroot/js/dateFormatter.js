/**
 * Date formatting utilities for Azurite UI
 * Provides culture-specific, timezone-aware date formatting with ISO-8601 fallback
 */

/**
 * Formats an ISO-8601 date string to a friendly, culture-specific format
 * in the browser's local timezone
 * @param {string} isoDateString - ISO-8601 formatted date string (e.g., "2025-11-13T10:30:00.000Z")
 * @returns {Object} Object with 'friendly' and 'iso' properties
 */
function formatFriendlyDate(isoDateString) {
    if (!isoDateString) {
        return { friendly: 'not set', iso: '' };
    }

    try {
        const date = new Date(isoDateString);

        // Check if date is valid
        if (isNaN(date.getTime())) {
            return { friendly: isoDateString, iso: isoDateString };
        }

        // Format date using browser's locale and timezone
        // This provides culture-specific formatting (e.g., MM/DD/YYYY for US, DD/MM/YYYY for UK)
        const options = {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false // Use 24-hour format; set to true for 12-hour with AM/PM
        };

        const friendlyDate = date.toLocaleString(undefined, options);

        return {
            friendly: friendlyDate,
            iso: isoDateString
        };
    } catch (error) {
        console.error('Error formatting date:', error);
        return { friendly: isoDateString, iso: isoDateString };
    }
}

/**
 * Creates an HTML string for a date with friendly display and ISO-8601 tooltip
 * @param {string} isoDateString - ISO-8601 formatted date string
 * @returns {string} HTML string with span element including title attribute
 */
function formatDateWithTooltip(isoDateString) {
    const formatted = formatFriendlyDate(isoDateString);

    if (!formatted.iso) {
        return formatted.friendly;
    }

    return `<span title="${escapeHtml(formatted.iso)}" style="cursor: help; border-bottom: 1px dotted #999;">${escapeHtml(formatted.friendly)}</span>`;
}

/**
 * Escapes HTML special characters to prevent XSS
 * @param {string} text - Text to escape
 * @returns {string} Escaped text
 */
function escapeHtml(text) {
    if (text === null || text === undefined) {
        return '';
    }

    const div = document.createElement('div');
    div.textContent = text.toString();
    return div.innerHTML;
}
