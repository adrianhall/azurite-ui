# Blob Upload

As part of AzuriteUI, we want the ability to upload blobs to a container, which includes the setting of properties including standard HTTP content headers, metadata, and tags.

* As a user, I should be able to upload blobs up to 10Gb.
* As a user, I should be able to see the progress of blob uploads.
* Chunks, partial, or full uploads should not be stored on the AzuriteUI container (pass through to Azurite).

The AzuriteService contains several methods for uploading chunked files, but these can be adjusted to whatever is required.  Similarly, the StorageRepository has a mirror of the AzuriteService methods for managing uploads along with updating the Cache database.
