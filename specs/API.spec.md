# API Specification

The API is used to access the AzuriteUI data model.  All APIs provide standard problem reporting.  All APIs can respond with the following:

* `400 Bad Request` - the input value was invalid.
* `404 Not Found` - an element on the path was not found.

## Endpoints

The following endpoints are available:

### ListContainers: `GET /api/containers`

An OData v4 endpoint for listing the containers:

#### ListContainers Request

**Query Parameters**:

* `$filter` - (optional) an OData v4 filter.
* `$orderBy` - (optional, default: `name asc`) an OData v4 ordering.
* `$select` - (optional) an OData v4 field selector.
* `$skip` - (optional, default: 0) the number of records to skip.
* `$top` - (optional, default: 25) the number of records to return.

**Headers**:

No additional request headers are supported.

#### ListContainers Response

**Status Codes**:

* 200 OK
* 400 Bad Request

**Response Headers**:

* `Content-Type: application/json`

**Body**:

```json
{
    "items": [
        { /* item 1 */ },
        { /* item 2 */ },
        ...
    ],
    "filteredCount": 0,
    "totalCount": 0,
    "nextLink": "$skip=10&$top=10",
    "prevLink": "$skip=0"
}
```

This is the JSON equivalent of the `PagedResponse<ContainerDTO>` model.

### CreateContainer: `POST /api/containers`

Creates a new (empty) container.

#### CreateContainer Request

**Headers**:

No additional request headers are supported.

**Body**:

```json
{
    "containerName": "name-of-container",
    "defaultEncryptionScope": "",
    "metadata": {
        "key1", "value1",
        "key2", "value2"
    },
    "preventEncryptionScopeOverride": false,
    "publicAccess": "none"
}
```

#### CreateContainer Response

**Status Codes**:

* 201 Created
* 400 Bad Request
* 409 Conflict

**Headers**:

* `ETag: "quoted-etag"`
* `Last-Modified: <rfc-1123 date/time stamp>`
* `Location: <location-of-new-resource>`
* `Content-Type: application/json`

**Body**:

```json
{
    "name": "name-of-container",
    "etag": "unquoted-etag",
    "lastModified": "iso-8601 utc timestamp",
    "blobCount": 0,
    "defaultEncryptionScope": "",
    "hasImmutabilityPolicy": false,
    "hasImmutableStorageWithVersioning": false,
    "hasLegalHold": false,
    "metadata": {
        "key1", "value1",
        "key2", "value2"
    },
    "preventEncryptionScopeOverride": false,
    "publicAccess": "none",
    "totalSize": 0
}
```

This matches the `ContainerDTO`

### GetContainerByName `GET /api/containers/{containerName}`

Retrieves the properties for a container:

#### GetContainerByName Request

**Headers**:

* `If-None-Match: "quoted-etag"`
* `If-Modified-Since: rfc-1123-date/time`

**Path Elements**:

* `{containerName}` - the name of the container to retrieve.

### GetContainerByName Response

**Status Codes**:

* 200 OK
* 304 Not Modified
* 400 Bad Request
* 404 Not Found

**Headers**:

* `ETag: "quoted-etag"`
* `Last-Modified: <rfc-1123 date/time stamp>`
* `Location: <location-of-new-resource>`
* `Content-Type: application/json`

**Body**:

```json
{
    "name": "name-of-container",
    "etag": "unquoted-etag",
    "lastModified": "iso-8601 utc timestamp",
    "blobCount": 0,
    "defaultEncryptionScope": "",
    "hasImmutabilityPolicy": false,
    "hasImmutableStorageWithVersioning": false,
    "hasLegalHold": false,
    "metadata": {
        "key1", "value1",
        "key2", "value2"
    },
    "preventEncryptionScopeOverride": false,
    "publicAccess": "none",
    "totalSize": 0
}
```

This matches the `ContainerDTO`

### DeleteContainer `DELETE /api/containers/{containerName}`

Deletes the container and all containing blobs.

#### DeleteContainer Request

**Headers**:

* `If-Match: "quoted-etag"`
* `If-Unmodified-Since: rfc-1123-date/time`

**Path Elements**:

* `{containerName}` - the name of the container to retrieve.

### DeleteContainer Response

**Status Codes**:

* 204 No Content
* 400 Bad Request
* 404 Not Found
* 412 Precondition Failed

### UpdateContainer `PUT /api/containers/{containerName}`

Replaces the metadata on a container with updated metadata.

#### UpdateContainer Request

**Headers**:

* `If-Match: "quoted-etag"`
* `If-Unmodified-Since: rfc-1123-date/time`

**Path Elements**:

* `{containerName}` - the name of the container to retrieve (note: either do not provide containerName in body, or make sure it matches).

**Body**:

```json
{
    "containerName": "name-of-container",
    "metadata": {
        "key1", "value1",
        "key2", "value2"
    }
}
```

#### UpdateContainer Response

**Status Codes**:

* 200 OK
* 400 Bad Request
* 404 Not Found
* 412 Precondition Failed

**Headers**:

* `ETag: "quoted-etag"`
* `Last-Modified: <rfc-1123 date/time stamp>`
* `Content-Type: application/json`

**Body**:

```json
{
    "name": "name-of-container",
    "etag": "unquoted-etag",
    "lastModified": "iso-8601 utc timestamp",
    "blobCount": 0,
    "defaultEncryptionScope": "",
    "hasImmutabilityPolicy": false,
    "hasImmutableStorageWithVersioning": false,
    "hasLegalHold": false,
    "metadata": {
        "key1", "value1",
        "key2", "value2"
    },
    "preventEncryptionScopeOverride": false,
    "publicAccess": "none",
    "totalSize": 0
}
```

This matches the `ContainerDTO`.

### ListBlobs: `GET /api/containers/{containerName}/blobs`

An OData v4 endpoint for listing the blobs within a container:

#### ListBlobs Request

**Path Elements**:

* `{containerName}` - the name of the container to list blobs from.

**Query Parameters**:

* `$filter` - (optional) an OData v4 filter.
* `$orderBy` - (optional, default: `name asc`) an OData v4 ordering.
* `$select` - (optional) an OData v4 field selector.
* `$skip` - (optional, default: 0) the number of records to skip.
* `$top` - (optional, default: 25) the number of records to return.

**Headers**:

No additional request headers are supported.

#### ListBlobs Response

**Status Codes**:

* 200 OK
* 400 Bad Request
* 404 Not Found

**Response Headers**:

* `Content-Type: application/json`

**Body**:

```json
{
    "items": [
        { /* item 1 */ },
        { /* item 2 */ },
        ...
    ],
    "filteredCount": 0,
    "totalCount": 0,
    "nextLink": "$skip=10&$top=10",
    "prevLink": "$skip=0"
}
```

This is the JSON equivalent of the `PagedResponse<BlobDTO>` model.

### GetBlobByName `GET /api/containers/{containerName}/blobs/{blobName}`

Retrieves the properties for a blob:

#### GetBlobByName Request

**Headers**:

* `If-None-Match: "quoted-etag"`
* `If-Modified-Since: rfc-1123-date/time`

**Path Elements**:

* `{containerName}` - the name of the container containing the blob.
* `{blobName}` - the name of the blob to retrieve.

#### GetBlobByName Response

**Status Codes**:

* 200 OK
* 304 Not Modified
* 400 Bad Request
* 404 Not Found

**Headers**:

* `ETag: "quoted-etag"`
* `Last-Modified: <rfc-1123 date/time stamp>`
* `Content-Type: application/json`

**Body**:

```json
{
    "name": "name-of-blob",
    "etag": "unquoted-etag",
    "lastModified": "iso-8601 utc timestamp",
    "blobType": "block",
    "containerName": "name-of-container",
    "contentEncoding": "",
    "contentLanguage": "",
    "contentLength": 0,
    "contentType": "application/octet-stream",
    "createdOn": "iso-8601 utc timestamp",
    "expiresOn": "iso-8601 utc timestamp",
    "hasLegalHold": false,
    "lastAccessedOn": "iso-8601 utc timestamp",
    "metadata": {
        "key1": "value1",
        "key2": "value2"
    },
    "tags": {
        "key1": "value1",
        "key2": "value2"
    },
    "remainingRetentionDays": 0
}
```

This matches the `BlobDTO`

### DeleteBlob `DELETE /api/containers/{containerName}/blobs/{blobName}`

Deletes the blob.

#### DeleteBlob Request

**Headers**:

* `If-Match: "quoted-etag"`
* `If-Unmodified-Since: rfc-1123-date/time`

**Path Elements**:

* `{containerName}` - the name of the container containing the blob.
* `{blobName}` - the name of the blob to delete.

#### DeleteBlob Response

**Status Codes**:

* 204 No Content
* 400 Bad Request
* 404 Not Found
* 412 Precondition Failed

### UpdateBlob `PUT /api/containers/{containerName}/blobs/{blobName}`

Updates the metadata and tags on a blob.

#### UpdateBlob Request

**Headers**:

* `If-Match: "quoted-etag"`
* `If-Unmodified-Since: rfc-1123-date/time`

**Path Elements**:

* `{containerName}` - the name of the container containing the blob (note: either do not provide containerName in body, or make sure it matches).
* `{blobName}` - the name of the blob to update (note: either do not provide blobName in body, or make sure it matches).

**Body**:

```json
{
    "containerName": "name-of-container",
    "blobName": "name-of-blob",
    "metadata": {
        "key1": "value1",
        "key2": "value2"
    },
    "tags": {
        "key1": "value1",
        "key2": "value2"
    }
}
```

#### UpdateBlob Response

**Status Codes**:

* 200 OK
* 400 Bad Request
* 404 Not Found
* 412 Precondition Failed

**Headers**:

* `ETag: "quoted-etag"`
* `Last-Modified: <rfc-1123 date/time stamp>`
* `Content-Type: application/json`

**Body**:

```json
{
    "name": "name-of-blob",
    "etag": "unquoted-etag",
    "lastModified": "iso-8601 utc timestamp",
    "blobType": "block",
    "containerName": "name-of-container",
    "contentEncoding": "",
    "contentLanguage": "",
    "contentLength": 0,
    "contentType": "application/octet-stream",
    "createdOn": "iso-8601 utc timestamp",
    "expiresOn": "iso-8601 utc timestamp",
    "hasLegalHold": false,
    "lastAccessedOn": "iso-8601 utc timestamp",
    "metadata": {
        "key1": "value1",
        "key2": "value2"
    },
    "tags": {
        "key1": "value1",
        "key2": "value2"
    },
    "remainingRetentionDays": 0
}
```

This matches the `BlobDTO`.

### DownloadBlob `GET /api/containers/{containerName}/blobs/{blobName}/content`

Downloads the content of a blob. Supports HTTP Range requests for partial content downloads.

#### DownloadBlob Request

**Headers**:

* `Range: bytes=<start>-<end>` (optional) - Request a specific byte range of the blob content.

**Path Elements**:

* `{containerName}` - the name of the container containing the blob.
* `{blobName}` - the name of the blob to download.

**Query Parameters**:

* `disposition` - (optional) Controls the Content-Disposition header. Valid values are:
  * `attachment` - Force download with filename
  * `inline` - Display in browser if possible

#### DownloadBlob Response

**Status Codes**:

* 200 OK - Full content returned
* 206 Partial Content - Partial content returned (Range request)
* 400 Bad Request - Invalid parameters
* 404 Not Found - Blob not found
* 416 Range Not Satisfiable - Requested range is invalid

**Headers**:

* `Accept-Ranges: bytes`
* `Content-Type: <blob-content-type>`
* `Content-Range: bytes <start>-<end>/<total>` (only for 206 responses)
* `Content-Disposition: <disposition>; filename="<blobName>"` (only if disposition query parameter provided)
* `ETag: "quoted-etag"`
* `Last-Modified: <rfc-1123 date/time stamp>`

**Body**:

The raw binary content of the blob (or the requested byte range).

## Upload Endpoints

The following endpoints handle chunked blob uploads, allowing uploads of files up to 10GB with progress tracking.

### CreateUpload: `POST /api/containers/{containerName}/blobs`

Initiates a new blob upload session for chunked uploads.

#### CreateUpload Request

**Path Elements**:

* `{containerName}` - the name of the container where the blob will be uploaded.

**Headers**:

* `Content-Type: application/json`

**Body**:

```json
{
    "blobName": "name-of-blob",
    "containerName": "name-of-container",
    "contentLength": 1048576,
    "contentType": "application/octet-stream",
    "contentEncoding": "",
    "contentLanguage": "",
    "metadata": {
        "key1": "value1",
        "key2": "value2"
    },
    "tags": {
        "key1": "value1",
        "key2": "value2"
    }
}
```

This matches the `CreateUploadRequestDTO`. Note: `containerName` in body must match the route parameter.

#### CreateUpload Response

**Status Codes**:

* 201 Created
* 400 Bad Request
* 404 Not Found (container not found)
* 409 Conflict (blob already exists)

**Headers**:

* `Location: /api/uploads/{uploadId}`
* `Content-Type: application/json`

**Body**:

```json
{
    "uploadId": "00000000-0000-0000-0000-000000000000",
    "containerName": "name-of-container",
    "blobName": "name-of-blob",
    "contentLength": 1048576,
    "contentType": "application/octet-stream",
    "uploadedBlocks": [],
    "uploadedLength": 0,
    "createdAt": "iso-8601 utc timestamp",
    "lastActivityAt": "iso-8601 utc timestamp"
}
```

This matches the `UploadStatusDTO`.

### ListUploads: `GET /api/uploads`

An OData v4 endpoint for listing all active upload sessions:

#### ListUploads Request

**Query Parameters**:

* `$filter` - (optional) an OData v4 filter.
* `$orderBy` - (optional, default: `lastActivityAt desc`) an OData v4 ordering.
* `$select` - (optional) an OData v4 field selector.
* `$skip` - (optional, default: 0) the number of records to skip.
* `$top` - (optional, default: 25) the number of records to return.

**Headers**:

No additional request headers are supported.

#### ListUploads Response

**Status Codes**:

* 200 OK
* 400 Bad Request

**Response Headers**:

* `Content-Type: application/json`

**Body**:

```json
{
    "items": [
        {
            "id": "00000000-0000-0000-0000-000000000000",
            "name": "name-of-blob",
            "containerName": "name-of-container",
            "lastActivityAt": "iso-8601 utc timestamp",
            "progress": 50.0
        },
        ...
    ],
    "filteredCount": 0,
    "totalCount": 0,
    "nextLink": "$skip=10&$top=10",
    "prevLink": "$skip=0"
}
```

This is the JSON equivalent of the `PagedResponse<UploadDTO>` model.

### GetUploadStatus: `GET /api/uploads/{uploadId}`

Retrieves the status of an upload session, including progress and uploaded blocks.

#### GetUploadStatus Request

**Path Elements**:

* `{uploadId}` - the unique identifier (GUID) of the upload session.

**Headers**:

No additional request headers are supported.

#### GetUploadStatus Response

**Status Codes**:

* 200 OK
* 400 Bad Request
* 404 Not Found

**Headers**:

* `Content-Type: application/json`

**Body**:

```json
{
    "uploadId": "00000000-0000-0000-0000-000000000000",
    "containerName": "name-of-container",
    "blobName": "name-of-blob",
    "contentLength": 1048576,
    "contentType": "application/octet-stream",
    "uploadedBlocks": ["YmxvY2sxMDE=", "YmxvY2sxMDI="],
    "uploadedLength": 524288,
    "createdAt": "iso-8601 utc timestamp",
    "lastActivityAt": "iso-8601 utc timestamp"
}
```

This matches the `UploadStatusDTO`. The `uploadedBlocks` array contains Base64-encoded block IDs.

### UploadBlock: `PUT /api/uploads/{uploadId}/blocks/{blockId}`

Uploads a block (chunk) of data for an upload session. The block data is streamed directly to Azure Storage without buffering in memory.

#### UploadBlock Request

**Path Elements**:

* `{uploadId}` - the unique identifier (GUID) of the upload session.
* `{blockId}` - the Base64-encoded block identifier (must be unique within the blob and max 64 bytes when decoded).

**Headers**:

* `Content-Type: application/octet-stream`
* `Content-Length: <size-in-bytes>` (required)
* `Content-MD5: <base64-md5-hash>` (optional)

**Body**:

The raw binary content of the block (up to 10GB).

#### UploadBlock Response

**Status Codes**:

* 200 OK
* 400 Bad Request (invalid block ID or validation failure)
* 404 Not Found (upload session not found)

**Headers**:

* `Content-Type: application/json`

**Body**:

```json
{
    "uploadId": "00000000-0000-0000-0000-000000000000",
    "blockId": "YmxvY2sxMDE=",
    "message": "Block uploaded successfully"
}
```

### CommitUpload: `PUT /api/uploads/{uploadId}/commit`

Commits an upload session by finalizing the blob with the specified block list. This operation will create the blob in Azure Storage and remove the upload session.

#### CommitUpload Request

**Path Elements**:

* `{uploadId}` - the unique identifier (GUID) of the upload session.

**Headers**:

* `Content-Type: application/json`

**Body**:

```json
{
    "blockIds": ["YmxvY2sxMDE=", "YmxvY2sxMDI=", "YmxvY2sxMDM="]
}
```

This matches the `CommitUploadRequestDTO`. The blocks are committed in the order specified.

#### CommitUpload Response

**Status Codes**:

* 200 OK
* 400 Bad Request (missing blocks or validation failure)
* 404 Not Found (upload session not found)

**Headers**:

* `Location: /api/containers/{containerName}/blobs/{blobName}`
* `ETag: "quoted-etag"`
* `Last-Modified: <rfc-1123 date/time stamp>`
* `Content-Type: application/json`

**Body**:

```json
{
    "name": "name-of-blob",
    "etag": "unquoted-etag",
    "lastModified": "iso-8601 utc timestamp",
    "blobType": "block",
    "containerName": "name-of-container",
    "contentEncoding": "",
    "contentLanguage": "",
    "contentLength": 1048576,
    "contentType": "application/octet-stream",
    "createdOn": "iso-8601 utc timestamp",
    "expiresOn": "iso-8601 utc timestamp",
    "hasLegalHold": false,
    "lastAccessedOn": "iso-8601 utc timestamp",
    "metadata": {
        "key1": "value1",
        "key2": "value2"
    },
    "tags": {
        "key1": "value1",
        "key2": "value2"
    },
    "remainingRetentionDays": 0
}
```

This matches the `BlobDTO`.

### CancelUpload: `DELETE /api/uploads/{uploadId}`

Cancels an upload session and removes it from the cache. Staged blocks in Azure Storage will automatically expire.

#### CancelUpload Request

**Path Elements**:

* `{uploadId}` - the unique identifier (GUID) of the upload session to cancel.

**Headers**:

No additional request headers are supported.

#### CancelUpload Response

**Status Codes**:

* 204 No Content
* 400 Bad Request
* 404 Not Found

### GetDashboard: `GET /api/dashboard`

Retrieves dashboard information including statistics about containers and blobs, and lists of recently modified containers and blobs.

#### GetDashboard Request

**Query Parameters**:

None.

**Headers**:

No additional request headers are supported.

#### GetDashboard Response

**Status Codes**:

* 200 OK
* 400 Bad Request

**Response Headers**:

* `Content-Type: application/json`

**Body**:

```json
{
    "stats": {
        "containers": 0,
        "blobs": 0,
        "totalBlobSize": 0,
        "totalImageSize": 0
    },
    "recentContainers": [
        {
            "name": "container-name",
            "lastModified": "iso-8601 utc timestamp",
            "blobCount": 0,
            "totalSize": 0
        }
    ],
    "recentBlobs": [
        {
            "name": "blob-name",
            "containerName": "container-name",
            "lastModified": "iso-8601 utc timestamp",
            "contentType": "application/octet-stream",
            "contentLength": 0
        }
    ]
}
```

This matches the `DashboardResponse` model, which includes:
- `stats` (DashboardStats): Statistics including total container count, blob count, total blob size, and total image size
- `recentContainers` (RecentContainerInfo[]): Up to 10 most recently modified containers, ordered by the most recent of either the container's last modified date or the last modified date of its most recent blob
- `recentBlobs` (RecentBlobInfo[]): Up to 10 most recently modified blobs, ordered by last modified date descending
