/**
 * BlobUploader - A reusable class for uploading blobs to Azure Storage using chunked uploads.
 *
 * Usage:
 *   const uploader = new BlobUploader(containerName, file);
 *   uploader.addEventListener('upload:progress', (e) => console.log(e.detail.percent));
 *   uploader.addEventListener('upload:complete', (e) => console.log('Done!', e.detail.blob));
 *   uploader.addEventListener('upload:error', (e) => console.error(e.detail.error));
 *   await uploader.upload();
 *
 * Events:
 *   - upload:start: { uploadId, containerName, blobName }
 *   - upload:progress: { percent, uploadedBytes, totalBytes, currentBlock, totalBlocks }
 *   - upload:complete: { blob: BlobDTO }
 *   - upload:error: { error, message }
 *   - upload:cancelled: { uploadId }
 */
class BlobUploader extends EventTarget {
    /**
     * Creates a new BlobUploader instance
     * @param {string} containerName - The name of the container to upload to
     * @param {File} file - The file to upload
     * @param {Object} options - Optional configuration
     * @param {number} options.chunkSize - Size of each chunk in bytes (default: 4MB)
     * @param {Object} options.metadata - Optional metadata for the blob
     * @param {Object} options.tags - Optional tags for the blob
     */
    constructor(containerName, file, options = {}) {
        super();
        this.containerName = containerName;
        this.file = file;
        this.chunkSize = options.chunkSize || (4 * 1024 * 1024); // 4MB default
        this.metadata = options.metadata || {};
        this.tags = options.tags || {};

        this.uploadId = null;
        this.abortController = null;
        this.uploadedBytes = 0;
        this.totalBytes = file.size;
        this.blockIds = [];
    }

    /**
     * Starts the upload process
     * @returns {Promise<BlobDTO>} The created blob information
     */
    async upload() {
        try {
            this.abortController = new AbortController();

            // Step 1: Create upload session
            await this._createUploadSession();

            // Step 2: Upload blocks
            await this._uploadBlocks();

            // Step 3: Commit upload
            const blob = await this._commitUpload();

            // Dispatch completion event
            this.dispatchEvent(new CustomEvent('upload:complete', {
                detail: { blob }
            }));

            return blob;
        } catch (error) {
            // If this was a cancellation, don't treat it as an error
            if (error.name === 'AbortError') {
                return;
            }

            // Try to clean up the upload session
            if (this.uploadId) {
                await this._cancelUploadSession().catch(() => {
                    // Ignore cleanup errors
                });
            }

            // Dispatch error event
            this.dispatchEvent(new CustomEvent('upload:error', {
                detail: {
                    error,
                    message: error.message || 'An error occurred during upload'
                }
            }));

            throw error;
        }
    }

    /**
     * Cancels the ongoing upload
     */
    async cancel() {
        if (this.abortController) {
            this.abortController.abort();
        }

        if (this.uploadId) {
            await this._cancelUploadSession();

            this.dispatchEvent(new CustomEvent('upload:cancelled', {
                detail: { uploadId: this.uploadId }
            }));
        }
    }

    /**
     * Creates an upload session with the API
     * @private
     */
    async _createUploadSession() {
        const contentType = this.file.type || 'application/octet-stream';

        const response = await fetch(`/api/containers/${encodeURIComponent(this.containerName)}/blobs`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                blobName: this.file.name,
                containerName: this.containerName,
                contentLength: this.file.size,
                contentType: contentType,
                metadata: this.metadata,
                tags: this.tags
            }),
            signal: this.abortController.signal
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to create upload session: ${error}`);
        }

        const uploadStatus = await response.json();
        this.uploadId = uploadStatus.uploadId;

        // Dispatch start event
        this.dispatchEvent(new CustomEvent('upload:start', {
            detail: {
                uploadId: this.uploadId,
                containerName: this.containerName,
                blobName: this.file.name
            }
        }));
    }

    /**
     * Uploads the file in chunks
     * @private
     */
    async _uploadBlocks() {
        const totalBlocks = Math.ceil(this.file.size / this.chunkSize);

        for (let blockIndex = 0; blockIndex < totalBlocks; blockIndex++) {
            const start = blockIndex * this.chunkSize;
            const end = Math.min(start + this.chunkSize, this.file.size);
            const chunk = this.file.slice(start, end);

            // Generate block ID (must be Base64 encoded and consistent)
            const blockIdString = `block-${String(blockIndex).padStart(5, '0')}`;
            const blockId = btoa(blockIdString);

            // Upload the block
            await this._uploadBlock(blockId, chunk);

            this.blockIds.push(blockId);
            this.uploadedBytes = end;

            // Dispatch progress event
            const percent = Math.round((this.uploadedBytes / this.totalBytes) * 100);
            this.dispatchEvent(new CustomEvent('upload:progress', {
                detail: {
                    percent,
                    uploadedBytes: this.uploadedBytes,
                    totalBytes: this.totalBytes,
                    currentBlock: blockIndex + 1,
                    totalBlocks
                }
            }));
        }
    }

    /**
     * Uploads a single block
     * @private
     */
    async _uploadBlock(blockId, chunk) {
        const response = await fetch(
            `/api/uploads/${this.uploadId}/blocks/${encodeURIComponent(blockId)}`,
            {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/octet-stream',
                    'Content-Length': chunk.size.toString()
                },
                body: chunk,
                signal: this.abortController.signal
            }
        );

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to upload block ${blockId}: ${error}`);
        }
    }

    /**
     * Commits the upload by finalizing the blob
     * @private
     */
    async _commitUpload() {
        const response = await fetch(`/api/uploads/${this.uploadId}/commit`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                blockIds: this.blockIds
            }),
            signal: this.abortController.signal
        });

        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to commit upload: ${error}`);
        }

        return await response.json();
    }

    /**
     * Cancels an upload session
     * @private
     */
    async _cancelUploadSession() {
        if (!this.uploadId) {
            return;
        }

        try {
            const response = await fetch(`/api/uploads/${this.uploadId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                console.error('Failed to cancel upload session:', await response.text());
            }
        } catch (error) {
            console.error('Error cancelling upload session:', error);
        }
    }
}
