# User Interface

The UI is a modern web application featuring:

* **Navigation**: Breadcrumb navigation hierarchy (Home > Containers > Container Name)
* **Date Display**: Relative dates ("2 hours ago") with absolute format on hover
* **Container View**: Tile-based list with infinite scroll (25 items per page default)
* **Blob View**: Paginated data table with configurable page sizes (10, 25, 50, 100 items)
* **Actions**: Context menus for delete, download, info operations
* **Modals**: Confirmation dialogs for destructive operations
* **Slide-out Panels**: Detailed property views for containers and blobs

## Error Handling

The application provides clear error feedback:

* Connection status indicator in the navbar
* User-friendly error messages when Azurite is unavailable
* Validation errors from Azurite are displayed to users
* Actionable error messages for operation failures

Underneath, the user interface uses Razor Pages with Bootstrap 5.

Error messages from async operation failures will be presented as dismissable toasts in the bottom right corner of the screen.

## Color Scheme

* Background: #c0c0c0
* Navigation Bar: white on #085f89
* Content area background : white
* Foreground: #1a1a1a

## Fonts

Uses the "Atlas Design" system:

* -apple-system, BlinkMacSystemFont, 'Segoe UI Variable Text', 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Fira Sans', 'Droid Sans', 'Helvetica Neue', Helvetica, Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji'

## Main Layout

The main layout consists of two parts:

```text
+---------------------------------------------------------------------------------------------+
| [Logo] Azurite UI                                                                  [Status] |
+---------------------------------------------------------------------------------------------+
|                                                                                             |
|                                                                                             |
|                                       Content Area                                          |
|                                                                                             |
|                                                                                             |
+---------------------------------------------------------------------------------------------+
```

The logo should be a separate component so that it is easily replaceable.  It uses the Bootstrap5 `box-seam` icon.

The status indicator should be a separate component.  It reflects the health of the system as reported by the /api/health endpoint.  The status indicator is a content area with a different background (based on the health) and an icon and text to indicate the health.

* When healthy, background is `success`, icon is `wifi` and text is `Connected`.
* When not healthy, background is `danger`, icon is `wifi-off`, and text is `Disconnected`.

The content area is bounded by `.container-xl` (i.e. centered area, 100% when lg or below, 1140px or 1320px width depending on how large the screen is).

## Dashboard

The dashboard provides basic information about the service.  It uses the same information as /api/dashboard endpoint.  The content area is made up as the following:

```text
[Home]                                                                               [+ Container]
+---------------------+  +---------------------+  +---------------------+  +---------------------+
|     Containers      |  |       Blobs         |  |      Total Size     |  |      Images Size    |
|         27          |  |       10242         |  |         100.1Gb     |  |        28.4Gb       |
+---------------------+  +---------------------+  +---------------------+  +---------------------+

+----------------------------------------------+  +----------------------------------------------+  
| Recently Updated Containers                  |  | Recently Updated Blobs                       |
| Name          | Last Modified | Count | Size |  | Name      | Container | Last Modified | Size |
+----------------------------------------------+  +----------------------------------------------+  
|                                              |  |                                              |  
|                                              |  |                                              |  
|                                              |  |                                              |  
|                                              |  |                                              |  
|                                              |  |                                              |  
|                                              |  |                                              |  
|                                              |  |                                              |  
|                                              |  |                                              |  
|                                              |  |                                              |  
+----------------------------------------------+  +----------------------------------------------+  
```

* All segments holding data will use testids for easy identification.
* [+ Container] will present a modal form asking for a container name, when provided will create a container (updating the UI)
* Each block is surrounded by a rounded corner box.
* Clicking on "Containers" takes the user to the containers list page.
* Clicking on a container in the "Recently updated containers" list takes the user to the blobs list page for that container.
* Clicking on a blob in the "Recently updated blobs" list take the user to the blobs list page (scrolled to the blob) and the get info panel is opened for that blob.
* Next to the Name in "Recently updated blobs" is an icon indicating the ContentType.

Refresh is manual.

## Containers Page

The containers page provides a sortable list of containers.

```text
[Home] > Containers                                                                  [+ Container]

+------------------------------------------------------------------------------------------------+
| Container                                             | Last Modified | Count | Size | Actions |
+------------------------------------------------------------------------------------------------+
|                                                                                                |
|                                                                                                |
|                                                                                                |
|                                                                                                |
|                                                                                                |
|                                                                                                |
|                                                                                                |
+------------------------------------------------------------------------------------------------+
```

The header is fixed (always visible) and the contents of the table scrolls.  By default, the list is sorted ascending by name.  However, the user can click on a column heading to sort the table another way.  All columns except for Actions are possible sort headings.  An icon next to the heading will indicate that it is the sort field and which direction (ascending / descending).  Use infinite scroll.

Clicking on the [+ Container] button (color: primary) will present a modal form asking for a container name, when provided will create a container (updating the UI)

There are three possible actions, represented by icons:

* `Delete container` (red `trash` icon) - displays a modal asking "are you sure you want to delete container "{name}" - default action is cancel.
* `Get info` (black `info-circle` icon) - Opens the info panel.
* `Browse` (black `folder2-open` icon) - Opens the Blobs Page for this container.

The Info panel is a panel that slides in from the right side of the screen.  It provides the ability to view all the information within the `ContainerDTO` object.  It provides the actions as buttons (contents of the button is the icon + word, color is the same as the icon in the list).

## Blobs Page

The blobs page provides a sortable list of blobs within the container.

```text
[Home] > Containers > {containerName}                                                   [+ Upload]

+------------------------------------------------------------------------------------------------+
| Name                                                  | Last Modified | Type  | Size | Actions |
+------------------------------------------------------------------------------------------------+
|                                                                                                |
|                                                                                                |
|                                                                                                |
|                                                                                                |
|                                                                                                |
|                                                                                                |
|                                                                                                |
+------------------------------------------------------------------------------------------------+
```

The header is fixed (always visible) and the contents of the table scrolls.  By default, the list is sorted ascending by name.  However, the user can click on a column heading to sort the table another way.  All columns except for Actions are possible sort headings.  An icon next to the heading will indicate that it is the sort field and which direction (ascending / descending).  Use infinite scroll.

Clicking on the [+ Upload] button (color: primary) will ask the user to select a file (probably within a modal form).  When selected and confirmed, the file will be uploaded, with progress reporting and cancellation.  Once the file is uploaded, the modal automatically closes and the list is updated.

There are three possible actions, represented by icons:

* `Delete blob` (red `trash` icon) - displays a modal asking "are you sure you want to delete blob "{name}" - default action is cancel.
* `Get info` (black `info-circle` icon) - Opens the info panel.
* `Download` (black `cloud-download` icon) - downloads the file with disposition "attachment" and the original filename.

The Info panel is a panel that slides in from the right side of the screen.  It provides the ability to view all the information within the `BlobDTO` object.  It provides the actions as buttons (contents of the button is the icon + word, color is the same as the icon in the list).
