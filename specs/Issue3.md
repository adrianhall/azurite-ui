# Issue 3 - Add metadata editor to Containers get info

Need to add the metadata editor to Containers get info panel.

1. Update containers get info panel in a similar way to the work we did on the blobs get info panel:

  * Add responsive width styling for the blob info panel:
    * Default (< 992px): 400px
    * Large screens (≥ 992px): 30%
    * Extra large (≥ 1400px): 40%
    * XXL screens (≥ 1920px): 50% (max 960px)
  * Add table styling with proper formatting

2. Update the Metadata area of the Containers get info panel:

  * Metadata is a series of key-value pairs.
  * Add an edit icon to move into "edit mode"
  * Allow user to add and remove key-value pairs.
  * Add a Save and Cancel button.
    * Cancel reverts (no changes)
    * Save calls PUT on /api/containers/{containerName} to save changes; then update the underlying data.
