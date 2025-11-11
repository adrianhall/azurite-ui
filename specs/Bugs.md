# Bug list from manual review

## Bug 1

Reproduction:

- Open browser to `http://localhost:8080/`
- Click on "+ Container"
- Create a container called test.
- Go to `http://localhost:8080/containers`
- Click on the "Get info" to open the info panel for the container test.
- Note the ETag value

Expected:

The ETag value is shown.

Actual:

The ETag value is not shown.

## Bug 2

Reproduction:

- Open browser to `http://localhost:8080/`
- Click on "+ Container"
- Create a container called test.
- Go to `http://localhost:8080/containers`
- Note the breadcrumb

Expected:

The start of the breadcrumb is the "Home" icon

Actual:

The start of the bradcrumb is the word "Home"

## Bug 3

Reproduction:

- Open browser to `http://localhost:8080/`
- Click on the "Containers" box (top left box on the content area).

Expected:

You should be moved to the containers list.  (See line 94 of [UI.spec.md](./UI.spec.md)).

Actual:

Box is not clickable.
