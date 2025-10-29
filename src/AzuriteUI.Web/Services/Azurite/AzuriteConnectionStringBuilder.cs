namespace AzuriteUI.Web.Services.Azurite;

/// <summary>
/// The parser for the Azurite connection string.
/// </summary>
public class AzuriteConnectionStringBuilder
{
    /// <summary>
    /// The accumulated properties for the connection string.
    /// </summary>
    internal Dictionary<string, string> _properties = [];

    /// <summary>
    /// Parses and validates the given Azurite connection string, returning a builder instance.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    /// <returns>The builder instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the connection string is null or whitespace.</exception>
    /// <exception cref="FormatException">Thrown if the connection string is invalid.</exception>
    public static AzuriteConnectionStringBuilder Parse(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new AzuriteConnectionStringBuilder();

        if (connectionString.TrimEnd(';').Equals(AzuriteKeys.DeveloperStorage, StringComparison.OrdinalIgnoreCase))
        {
            // Use the default Azurite development storage settings.
            builder
                .WithProtocol("http")
                .WithAccountName("devstoreaccount1")
                .WithAccountKey("Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==")
                .WithBlobEndpoint("http://127.0.0.1:10000/devstoreaccount1")
                .WithQueueEndpoint("http://127.0.0.1:10001/devstoreaccount1")
                .WithTableEndpoint("http://127.0.0.1:10002/devstoreaccount1");
            return builder;
        }

        var kvPairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var kvPair in kvPairs)
        {
            var kv = kvPair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2)
            {
                throw new FormatException($"Invalid key-value pair in connection string: '{kvPair}'.");
            }

            switch (kv[0])
            {
                case AzuriteKeys.DefaultEndpointsProtocol:
                    builder.WithProtocol(kv[1]);
                    break;
                case AzuriteKeys.AccountName:
                    builder.WithAccountName(kv[1]);
                    break;
                case AzuriteKeys.AccountKey:
                    builder.WithAccountKey(kv[1]);
                    break;
                case AzuriteKeys.BlobEndpoint:
                    builder.WithBlobEndpoint(kv[1]);
                    break;
                case AzuriteKeys.QueueEndpoint:
                    builder.WithQueueEndpoint(kv[1]);
                    break;
                case AzuriteKeys.TableEndpoint:
                    builder.WithTableEndpoint(kv[1]);
                    break;
                default:
                    throw new FormatException($"Unknown key in connection string: '{kv[0]}'.");
            }
        }

        return builder;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="AzuriteConnectionStringBuilder"/> class.
    /// </summary>
    public AzuriteConnectionStringBuilder()
    {
    }

    /// <summary>
    /// Sets the DefaultEndpointsProtocol property for the connection string.
    /// </summary>
    /// <param name="protocol">The protocol property.</param>
    /// <remarks>
    /// The DefaultEndpointsProtocol property specifies the protocol used to access the storage services.
    /// Valid values are 'http' and 'https'.  If not specified, the default is taken from the protocol
    /// part of the BlobEndppoint URI.
    /// </remarks>
    /// <returns>The modified builder (for chaining).</returns>
    /// <exception cref="ArgumentException">Thrown if the protocol is invalid.</exception>
    public AzuriteConnectionStringBuilder WithProtocol(string protocol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        if (_properties.ContainsKey(AzuriteKeys.DefaultEndpointsProtocol))
        {
            throw new ArgumentException("The DefaultEndpointsProtocol property has already been set.");
        }

        if (!protocol.Equals("http", StringComparison.OrdinalIgnoreCase) && !protocol.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The DefaultEndpointsProtocol property must be either 'http' or 'https'.");
        }

        _properties[AzuriteKeys.DefaultEndpointsProtocol] = protocol.ToLowerInvariant();
        return this;
    }

    /// <summary>
    /// Sets the AccountName property for the connection string.
    /// </summary>
    /// <param name="accountName">The account name property.</param>
    /// <returns>The modified builder (for chaining).</returns>
    /// <remarks>
    /// The AccountName property specifies the name of the storage account.  It can be between 3 and 24 characters,
    /// must start with a lowercase letter, and can contain only lowercase letters and numbers.  It is required.
    /// </remarks>
    /// <exception cref="ArgumentException">If the account name is invalid.</exception>
    public AzuriteConnectionStringBuilder WithAccountName(string accountName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        if (_properties.ContainsKey(AzuriteKeys.AccountName))
        {
            throw new ArgumentException("The AccountName property has already been set.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(accountName, "^[a-z][a-z0-9]{2,23}$"))
        {
            throw new ArgumentException("The AccountName must be 3-24 characters, start with a lowercase letter, and contain only lowercase letters and numbers.");
        }

        _properties[AzuriteKeys.AccountName] = accountName;
        return this;
    }

    /// <summary>
    /// Sets the AccountKey property for the connection string.
    /// </summary>
    /// <param name="accountKey">The account key property.</param>
    /// <returns>The modified builder (for chaining).</returns>
    /// <remarks>
    /// The AccountKey property specifies the key for the storage account.  It is required.  It must be a valid
    /// base64-encoded string.  Do not decode the string - it must be passed to the storage client libraries as-is.
    /// </remarks>
    /// <exception cref="ArgumentException">If the account key is invalid.</exception>
    public AzuriteConnectionStringBuilder WithAccountKey(string accountKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountKey);
        if (_properties.ContainsKey(AzuriteKeys.AccountKey))
        {
            throw new ArgumentException("The AccountKey property has already been set.");
        }

        // Validate that it's a valid base64 string
        try
        {
            Convert.FromBase64String(accountKey);
        }
        catch (FormatException)
        {
            throw new ArgumentException("The AccountKey must be a valid base64-encoded string.");
        }

        _properties[AzuriteKeys.AccountKey] = accountKey;
        return this;
    }

    /// <summary>
    /// Sets the BlobEndpoint property for the connection string.
    /// </summary>
    /// <param name="blobEndpoint">The blob endpoint property.</param>
    /// <returns>The modified builder (for chaining).</returns>
    /// <remarks>
    /// The BlobEndpoint property specifies the endpoint URI for the blob service.  It must be a valid URI and
    /// be either http or https.  It is required.  If the DefaultEndpointsProtocol property is not specified,
    /// this is used to determine the protocol.
    /// </remarks>
    /// <exception cref="ArgumentException">If the blob endpoint is invalid.</exception>
    public AzuriteConnectionStringBuilder WithBlobEndpoint(string blobEndpoint)
        => WithEndpoint(AzuriteKeys.BlobEndpoint, blobEndpoint);

    /// <summary>
    /// Sets the QueueEndpoint property for the connection string.
    /// </summary>
    /// <param name="queueEndpoint">The queue endpoint property.</param>
    /// <returns>The modified builder (for chaining).</returns>
    /// <remarks>
    /// The QueueEndpoint property specifies the endpoint URI for the queue service.  It must be a valid URI and
    /// be either http or https if specified.  It is optional.  If not specified, it is not included in the
    /// connection string.
    /// </remarks>
    /// <exception cref="ArgumentException">If the queue endpoint is invalid.</exception>
    public AzuriteConnectionStringBuilder WithQueueEndpoint(string queueEndpoint)
        => WithEndpoint(AzuriteKeys.QueueEndpoint, queueEndpoint);

    /// <summary>
    /// Sets the TableEndpoint property for the connection string.
    /// </summary>
    /// <param name="tableEndpoint">The table endpoint property.</param>
    /// <returns>The modified builder (for chaining).</returns>
    /// <remarks>
    /// The TableEndpoint property specifies the endpoint URI for the table service.  It must be a valid URI and
    /// be either http or https if specified.  It is optional.  If not specified, it is not included in the
    /// connection string.
    /// </remarks>
    /// <exception cref="ArgumentException">If the table endpoint is invalid.</exception>
    public AzuriteConnectionStringBuilder WithTableEndpoint(string tableEndpoint)
        => WithEndpoint(AzuriteKeys.TableEndpoint, tableEndpoint);

    /// <summary>
    /// An internal helper to set an endpoint property.
    /// </summary>
    /// <param name="key">The key within the connection string to set.</param>
    /// <param name="endpoint">The endpoint value to set.</param>
    /// <returns>The modified builder (for chaining).</returns>
    /// <exception cref="ArgumentException">If the endpoint is invalid.</exception>
    internal AzuriteConnectionStringBuilder WithEndpoint(string key, string endpoint)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        if (_properties.ContainsKey(key))
        {
            throw new ArgumentException($"The {key} property has already been set.");
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"The {key} must be a valid URI.");
        }

        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            throw new ArgumentException($"The {key} must use either http or https protocol.");
        }

        _properties[key] = endpoint;
        return this;
    }

    /// <summary>
    /// Returns the normalized form of the connection string.
    /// </summary>
    /// <remarks>
    /// The AccountName, AccountKey, and BlobEndpoint properties are required.  If the DefaultEndpointsProtocol
    /// property is not specified, it is derived from the BlobEndpoint property.  The QueueEndpoint and
    /// TableEndpoint properties are optional and only included if specified.
    /// </remarks>
    /// <returns>The connection string.</returns>
    /// <exception cref="FormatException">Thrown if the required components are missing.</exception>
    public override string ToString()
    {
        // Throw if any of the AzuriteKeys.RequiredKeys are missing
        var missingKeys = AzuriteKeys.RequiredKeys.Where(k => !_properties.ContainsKey(k)).ToList();
        if (missingKeys.Count != 0)
        {
            throw new FormatException($"The connection string is missing required properties: {string.Join(", ", missingKeys)}.");
        }

        // If DefaultEndpointsProtocol is not set, derive it from BlobEndpoint
        var protocol = _properties.TryGetValue(AzuriteKeys.DefaultEndpointsProtocol, out string? protocolValue)
            ? protocolValue : new Uri(_properties[AzuriteKeys.BlobEndpoint]).Scheme;

        // Build the connection string in a consistent order
        List<string> parts = [];
        foreach (var key in AzuriteKeys.AllKeys)
        {
            // Ensure the DefaultEndpointsProtocol is always included and uses our version.
            if (key.Equals(AzuriteKeys.DefaultEndpointsProtocol, StringComparison.OrdinalIgnoreCase))
            {
                parts.Add($"{AzuriteKeys.DefaultEndpointsProtocol}={protocol}");
            }
            else if (_properties.TryGetValue(key, out string? value))
            {
                parts.Add($"{key}={value}");
            }
        }

        // Join and return. 
        return string.Join(";", parts);
    }

    /// <summary>
    /// A list of all the Azurite keys in use.
    /// </summary>
    internal static class AzuriteKeys
    {
        /// <summary>
        /// The developer storage connection string.  This is substituted with the default Azurite development storage settings.
        /// </summary>
        public const string DeveloperStorage = "UseDevelopmentStorage=true";

        /// <summary>
        /// The protocol used to access the storage services.
        /// </summary>
        public const string DefaultEndpointsProtocol = "DefaultEndpointsProtocol";

        /// <summary>
        /// The name of the storage account.
        /// </summary>
        public const string AccountName = "AccountName";

        /// <summary>
        /// The key for the storage account.
        /// </summary>
        public const string AccountKey = "AccountKey";

        /// <summary>
        /// The endpoint URI for the blob service.
        /// </summary>
        public const string BlobEndpoint = "BlobEndpoint";

        /// <summary>
        /// The endpoint URI for the queue service.
        /// </summary>
        public const string QueueEndpoint = "QueueEndpoint";

        /// <summary>
        /// The endpoint URI for the table service.
        /// </summary>
        public const string TableEndpoint = "TableEndpoint";

        /// <summary>
        /// The list of required keys in the connection string.
        /// </summary>
        public static readonly IReadOnlyList<string> RequiredKeys = [AccountKey, AccountName, BlobEndpoint];

        /// <summary>
        /// The list of all keys in the connection string, in the order we expect them to appear.
        /// </summary>
        public static readonly IReadOnlyList<string> AllKeys = [
            DefaultEndpointsProtocol,
            AccountName,
            AccountKey,
            BlobEndpoint,
            QueueEndpoint,
            TableEndpoint
        ];

    }
}
