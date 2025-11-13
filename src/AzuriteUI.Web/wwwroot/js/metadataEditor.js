/**
 * Metadata Editor - Reusable component for editing key-value metadata
 * Can be used for both containers and blobs
 */

class MetadataEditor {
    constructor(containerId, metadata, onSave, onCancel) {
        this.containerId = containerId;
        this.originalMetadata = metadata || {};
        this.currentMetadata = JSON.parse(JSON.stringify(this.originalMetadata)); // Deep clone
        this.onSave = onSave;
        this.onCancel = onCancel;
        this.isEditMode = false;
        this.container = null;
    }

    /**
     * Initialize and render the editor in the specified container
     */
    render() {
        this.container = document.getElementById(this.containerId);
        if (!this.container) {
            console.error(`Container with id "${this.containerId}" not found`);
            return;
        }

        if (this.isEditMode) {
            this.renderEditMode();
        } else {
            this.renderViewMode();
        }
    }

    /**
     * Render view mode - displays metadata as a two-column table with edit icon
     */
    renderViewMode() {
        const metadataEntries = Object.entries(this.currentMetadata);

        let html = `
            <table class="metadata-table">
                <thead>
                    <tr>
                        <th>Key</th>
                        <th>Value</th>
                        <th style="width: 40px; text-align: center;">
                            <i class="bi bi-pencil action-icon metadata-edit-button"
                               title="Edit metadata" style="cursor: pointer;"></i>
                        </th>
                    </tr>
                </thead>
                <tbody>
        `;

        if (metadataEntries.length === 0) {
            html += `
                <tr>
                    <td colspan="3" class="text-muted text-center">No metadata</td>
                </tr>
            `;
        } else {
            metadataEntries.forEach(([key, value]) => {
                html += `
                    <tr>
                        <td>${this.escapeHtml(key)}</td>
                        <td>${this.escapeHtml(value)}</td>
                        <td></td>
                    </tr>
                `;
            });
        }

        html += `
                </tbody>
            </table>
        `;

        this.container.innerHTML = html;

        // Attach event listener for edit button
        const editButton = this.container.querySelector('.metadata-edit-button');
        if (editButton) {
            editButton.addEventListener('click', () => this.toggleEditMode());
        }
    }

    /**
     * Render edit mode - displays editable inputs with add/remove functionality
     */
    renderEditMode() {
        const metadataEntries = Object.entries(this.currentMetadata);

        let html = `
            <div class="metadata-edit-header">
                <small class="text-muted">Edit metadata key-value pairs</small>
            </div>
            <table class="metadata-table">
                <thead>
                    <tr>
                        <th>Key</th>
                        <th>Value</th>
                        <th style="width: 50px;"></th>
                    </tr>
                </thead>
                <tbody class="metadata-rows">
        `;

        metadataEntries.forEach(([key, value], index) => {
            html += this.renderMetadataRow(key, value, index);
        });

        html += `
                </tbody>
            </table>
            <div class="mt-2">
                <button type="button" class="btn btn-sm btn-outline-secondary metadata-add-button">
                    <i class="bi bi-plus-circle-fill me-1"></i> Add
                </button>
            </div>
            <div class="metadata-edit-actions">
                <button type="button" class="btn btn-sm btn-primary metadata-save-button">
                    <i class="bi bi-check-circle me-1"></i> Save
                </button>
                <button type="button" class="btn btn-sm btn-secondary metadata-cancel-button">
                    <i class="bi bi-x-circle me-1"></i> Cancel
                </button>
            </div>
        `;

        this.container.innerHTML = html;

        // Attach event listeners for buttons
        const addButton = this.container.querySelector('.metadata-add-button');
        if (addButton) {
            addButton.addEventListener('click', () => this.addRow());
        }

        const saveButton = this.container.querySelector('.metadata-save-button');
        if (saveButton) {
            saveButton.addEventListener('click', () => this.save());
        }

        const cancelButton = this.container.querySelector('.metadata-cancel-button');
        if (cancelButton) {
            cancelButton.addEventListener('click', () => this.cancel());
        }

        // Attach event listeners for remove buttons
        const removeButtons = this.container.querySelectorAll('.metadata-remove-button');
        removeButtons.forEach((button, index) => {
            button.addEventListener('click', () => this.removeRow(index));
        });
    }

    /**
     * Render a single metadata row in edit mode
     */
    renderMetadataRow(key, value, index) {
        return `
            <tr data-row-index="${index}">
                <td>
                    <input type="text" class="metadata-key" value="${this.escapeHtml(key)}"
                           placeholder="Key" data-row-index="${index}" />
                </td>
                <td>
                    <input type="text" class="metadata-value" value="${this.escapeHtml(value)}"
                           placeholder="Value" data-row-index="${index}" />
                </td>
                <td class="text-center">
                    <i class="bi bi-dash-circle-fill action-icon text-danger metadata-remove-button"
                       data-row-index="${index}"
                       title="Remove" style="cursor: pointer;"></i>
                </td>
            </tr>
        `;
    }

    /**
     * Toggle between view and edit modes
     */
    toggleEditMode() {
        this.isEditMode = !this.isEditMode;

        if (this.isEditMode) {
            // Reset current metadata to original when entering edit mode
            this.currentMetadata = JSON.parse(JSON.stringify(this.originalMetadata));
        }

        this.render();
    }

    /**
     * Add a new empty row in edit mode
     */
    addRow() {
        const tbody = this.container.querySelector('.metadata-rows');
        if (!tbody) return;

        const index = tbody.children.length;
        const newRow = document.createElement('tr');
        newRow.setAttribute('data-row-index', index);
        newRow.innerHTML = `
            <td>
                <input type="text" class="metadata-key" value=""
                       placeholder="Key" data-row-index="${index}" />
            </td>
            <td>
                <input type="text" class="metadata-value" value=""
                       placeholder="Value" data-row-index="${index}" />
            </td>
            <td class="text-center">
                <i class="bi bi-dash-circle-fill action-icon text-danger metadata-remove-button"
                   data-row-index="${index}"
                   title="Remove" style="cursor: pointer;"></i>
            </td>
        `;
        tbody.appendChild(newRow);

        // Attach event listener to the new remove button
        const removeButton = newRow.querySelector('.metadata-remove-button');
        if (removeButton) {
            removeButton.addEventListener('click', () => this.removeRow(index));
        }
    }

    /**
     * Remove a row by index
     */
    removeRow(index) {
        const tbody = this.container.querySelector('.metadata-rows');
        if (!tbody) return;

        const row = tbody.querySelector(`tr[data-row-index="${index}"]`);
        if (row) {
            row.remove();
        }
    }

    /**
     * Validate metadata inputs
     */
    validate() {
        const rows = this.container.querySelectorAll('.metadata-rows tr');
        const keys = [];
        const errors = [];

        rows.forEach((row, index) => {
            const keyInput = row.querySelector('.metadata-key');
            const valueInput = row.querySelector('.metadata-value');

            if (!keyInput || !valueInput) return;

            const key = keyInput.value.trim();
            const value = valueInput.value.trim();

            // Check for empty keys (but allow empty values)
            if (key === '' && value !== '') {
                errors.push(`Row ${index + 1}: Key cannot be empty`);
                keyInput.classList.add('is-invalid');
            } else {
                keyInput.classList.remove('is-invalid');
            }

            // Check for duplicate keys
            if (key !== '') {
                if (keys.includes(key)) {
                    errors.push(`Row ${index + 1}: Duplicate key "${key}"`);
                    keyInput.classList.add('is-invalid');
                } else {
                    keys.push(key);
                }
            }
        });

        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    /**
     * Collect metadata from input fields
     */
    collectMetadata() {
        const rows = this.container.querySelectorAll('.metadata-rows tr');
        const metadata = {};

        rows.forEach(row => {
            const keyInput = row.querySelector('.metadata-key');
            const valueInput = row.querySelector('.metadata-value');

            if (!keyInput || !valueInput) return;

            const key = keyInput.value.trim();
            const value = valueInput.value.trim();

            // Only add non-empty keys
            if (key !== '') {
                metadata[key] = value;
            }
        });

        return metadata;
    }

    /**
     * Save metadata changes
     */
    async save() {
        // Validate inputs
        const validation = this.validate();
        if (!validation.isValid) {
            alert('Validation errors:\n' + validation.errors.join('\n'));
            return;
        }

        // Collect metadata
        const newMetadata = this.collectMetadata();

        // Call the save callback
        try {
            const success = await this.onSave(newMetadata);
            if (success) {
                // Update original and current metadata
                this.originalMetadata = newMetadata;
                this.currentMetadata = JSON.parse(JSON.stringify(newMetadata));
                // Switch back to view mode
                this.isEditMode = false;
                this.render();
            }
        } catch (error) {
            console.error('Error saving metadata:', error);
            alert('Failed to save metadata. Please try again.');
        }
    }

    /**
     * Cancel editing and revert to original metadata
     */
    cancel() {
        if (this.onCancel) {
            this.onCancel();
        }

        // Reset to original metadata
        this.currentMetadata = JSON.parse(JSON.stringify(this.originalMetadata));
        this.isEditMode = false;
        this.render();
    }

    /**
     * Update the metadata and re-render (call this when data changes externally)
     */
    updateMetadata(newMetadata) {
        this.originalMetadata = newMetadata || {};
        this.currentMetadata = JSON.parse(JSON.stringify(this.originalMetadata));
        this.render();
    }

    /**
     * Escape HTML to prevent XSS
     */
    escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return String(text).replace(/[&<>"']/g, m => map[m]);
    }
}

// Note: The instance will be created by the calling page
// Example usage:
// const editor = new MetadataEditor('container-id', metadata, onSaveCallback, onCancelCallback);
// editor.render();
