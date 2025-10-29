# Spec for AzuriteConnectionStringBuilder

Namespace: AzuriteUI.Web.Services.Azurite
Class: AzuriteConnectionStringBuilder.cs

This class should both parse and validate the components of an Azurite connection string.  This is composed of multiple parts:

* `DefaultEndpointsProtocol` - optional, default: protocol section of the BlobEndpoint, values: 'http' or 'https'
* `BlobEndpoint` - required, value: a valid HTTP or HTTPS URI.
* `AccountName` - required, value: `^[a-z][a-z0-9]{2,23}` - 3-24 characters with an initial lowercase letter and then lowercase letters and numbers only.
* `AccountKey` - required, value: a base-64 encoded value.
* `QueueEndpoint` - optional, value: a valid HTTP or HTTPS URI.
* `TableEndpoint` - optional, value: a valid HTTP or HTTPS URI.

In addition, `UseDevelopmentStorage=true` (with or without a trailing semi-colon) should be replaced with the default connection string for Azurite.

## Validation and Parsing

* Keys can be mentioned once (and only once) - if a key is duplicated, throw a FormatException.
* Keys must have values if specified - if not, throw a FormatException
* Keys must be within the provided set - if not, throw a FormatException
* Values of the key must match the requirements - if not, throw a FormatException

## Typical usage

```csharp

// Parse a connection string
var connectionString = AzuriteConnectionStringBuilder.Parse(configuration.GetConnectionString("Azurite")).ToString();

// Build a connection string
var connectionString = new AzuriteConnectionStringBuilder()
    .WithProtocol("http")
    .WithAccountName("devstoreaccount1")
    .WithAccountKey("Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==")
    .WithBlobEndpoint("http://127.0.0.1:10000/devstoreaccount1")
    .WithQueueEndpoint("http://127.0.0.1:10001/devstoreaccount1")
    .WithTableEndpoint("http://127.0.0.1:10002/devstoreaccount1")
    .ToString();

```

Note that `.ToString()` throws a FormatException if a required property is not set.  The various `.With*()` methods throw a FormatException if they detect a duplicate key.
