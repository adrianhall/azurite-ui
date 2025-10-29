using AzuriteUI.Web.Services.Azurite;

namespace AzuriteUI.Web.UnitTests.Services.Azurite;

[ExcludeFromCodeCoverage]
public class AzuriteConnectionStringBuilder_Tests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateEmptyBuilder()
    {
        // Arrange & Act
        var builder = new AzuriteConnectionStringBuilder();

        // Assert
        builder.Should().NotBeNull();
        builder._properties.Should().BeEmpty();
    }

    #endregion

    #region Parse Method Tests

    [Fact]
    public void Parse_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        string? connectionString = null;

        // Act
        Action act = () => AzuriteConnectionStringBuilder.Parse(connectionString!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionString = "";

        // Act
        Action act = () => AzuriteConnectionStringBuilder.Parse(connectionString);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WithWhitespaceConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionString = "   ";

        // Act
        Action act = () => AzuriteConnectionStringBuilder.Parse(connectionString);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_WithDevelopmentStorageString_ShouldReturnBuilderWithDefaultSettings()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true";

        // Act
        var builder = AzuriteConnectionStringBuilder.Parse(connectionString);

        // Assert
        builder.Should().NotBeNull();
        builder._properties["AccountName"].Should().Be("devstoreaccount1");
        builder._properties["AccountKey"].Should().Be("Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");
        builder._properties["BlobEndpoint"].Should().Be("http://127.0.0.1:10000/devstoreaccount1");
        builder._properties["QueueEndpoint"].Should().Be("http://127.0.0.1:10001/devstoreaccount1");
        builder._properties["TableEndpoint"].Should().Be("http://127.0.0.1:10002/devstoreaccount1");
        builder._properties["DefaultEndpointsProtocol"].Should().Be("http");
    }

    [Fact]
    public void Parse_WithDevelopmentStorageStringAndTrailingSemicolon_ShouldReturnBuilderWithDefaultSettings()
    {
        // Arrange
        var connectionString = "UseDevelopmentStorage=true;";

        // Act
        var builder = AzuriteConnectionStringBuilder.Parse(connectionString);

        // Assert
        builder.Should().NotBeNull();
        builder._properties["AccountName"].Should().Be("devstoreaccount1");
    }

    [Fact]
    public void Parse_WithValidConnectionString_ShouldParseAllProperties()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=http;AccountName=testaccount;AccountKey=dGVzdGtleQ==;BlobEndpoint=http://localhost:10000/testaccount;QueueEndpoint=http://localhost:10001/testaccount;TableEndpoint=http://localhost:10002/testaccount";

        // Act
        var builder = AzuriteConnectionStringBuilder.Parse(connectionString);

        // Assert
        builder._properties["DefaultEndpointsProtocol"].Should().Be("http");
        builder._properties["AccountName"].Should().Be("testaccount");
        builder._properties["AccountKey"].Should().Be("dGVzdGtleQ==");
        builder._properties["BlobEndpoint"].Should().Be("http://localhost:10000/testaccount");
        builder._properties["QueueEndpoint"].Should().Be("http://localhost:10001/testaccount");
        builder._properties["TableEndpoint"].Should().Be("http://localhost:10002/testaccount");
    }

    [Fact]
    public void Parse_WithInvalidKeyValuePair_ShouldThrowFormatException()
    {
        // Arrange
        var connectionString = "InvalidPair;AccountName=testaccount";

        // Act
        Action act = () => AzuriteConnectionStringBuilder.Parse(connectionString);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*Invalid key-value pair*");
    }

    [Fact]
    public void Parse_WithUnknownKey_ShouldThrowFormatException()
    {
        // Arrange
        var connectionString = "UnknownKey=value;AccountName=testaccount";

        // Act
        Action act = () => AzuriteConnectionStringBuilder.Parse(connectionString);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*Unknown key*");
    }

    #endregion

    #region WithProtocol Method Tests

    [Fact]
    public void WithProtocol_WithHttp_ShouldSetProtocolToHttp()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        var result = builder.WithProtocol("http");

        // Assert
        result.Should().BeSameAs(builder);
        builder._properties["DefaultEndpointsProtocol"].Should().Be("http");
    }

    [Fact]
    public void WithProtocol_WithHttps_ShouldSetProtocolToHttps()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        var result = builder.WithProtocol("https");

        // Assert
        result.Should().BeSameAs(builder);
        builder._properties["DefaultEndpointsProtocol"].Should().Be("https");
    }

    [Fact]
    public void WithProtocol_WithUppercaseHttp_ShouldSetProtocolToLowercaseHttp()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        var result = builder.WithProtocol("HTTP");

        // Assert
        builder._properties["DefaultEndpointsProtocol"].Should().Be("http");
    }

    [Fact]
    public void WithProtocol_WithNull_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithProtocol(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithProtocol_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithProtocol("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithProtocol_WithInvalidProtocol_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithProtocol("ftp");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be either 'http' or 'https'*");
    }

    [Fact]
    public void WithProtocol_CalledTwice_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithProtocol("http");

        // Act
        Action act = () => builder.WithProtocol("https");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already been set*");
    }

    #endregion

    #region WithAccountName Method Tests

    [Fact]
    public void WithAccountName_WithValidName_ShouldSetAccountName()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        var result = builder.WithAccountName("testaccount");

        // Assert
        result.Should().BeSameAs(builder);
        builder._properties["AccountName"].Should().Be("testaccount");
    }

    [Fact]
    public void WithAccountName_WithMinimumLength_ShouldSetAccountName()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        var result = builder.WithAccountName("abc");

        // Assert
        builder._properties["AccountName"].Should().Be("abc");
    }

    [Fact]
    public void WithAccountName_WithMaximumLength_ShouldSetAccountName()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();
        const string accountName = "a12345678901234567890123"; // 25 characters but only 24 allowed

        // Act - Let's test the valid 24 character version
        var result = builder.WithAccountName(accountName); // 24 characters

        // Assert
        builder._properties["AccountName"].Should().Be(accountName);
    }

    [Fact]
    public void WithAccountName_WithNull_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountName(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithAccountName_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountName("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithAccountName_WithTooShortName_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountName("ab");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be 3-24 characters*");
    }

    [Fact]
    public void WithAccountName_WithTooLongName_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountName("a1234567890123456789012345"); // 26 characters

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be 3-24 characters*");
    }

    [Fact]
    public void WithAccountName_WithUppercaseLetter_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountName("TestAccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase letters and numbers*");
    }

    [Fact]
    public void WithAccountName_StartingWithNumber_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountName("1testaccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*start with a lowercase letter*");
    }

    [Fact]
    public void WithAccountName_WithSpecialCharacters_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountName("test-account");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase letters and numbers*");
    }

    [Fact]
    public void WithAccountName_CalledTwice_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount");

        // Act
        Action act = () => builder.WithAccountName("anotheraccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already been set*");
    }

    #endregion

    #region WithAccountKey Method Tests

    [Fact]
    public void WithAccountKey_WithValidBase64Key_ShouldSetAccountKey()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();
        var validKey = "dGVzdGtleQ=="; // "testkey" in base64

        // Act
        var result = builder.WithAccountKey(validKey);

        // Assert
        result.Should().BeSameAs(builder);
        builder._properties["AccountKey"].Should().Be(validKey);
    }

    [Fact]
    public void WithAccountKey_WithLongBase64Key_ShouldSetAccountKey()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();
        var validKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

        // Act
        var result = builder.WithAccountKey(validKey);

        // Assert
        builder._properties["AccountKey"].Should().Be(validKey);
    }

    [Fact]
    public void WithAccountKey_WithNull_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountKey(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithAccountKey_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountKey("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithAccountKey_WithInvalidBase64_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithAccountKey("not-valid-base64!");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*valid base64-encoded string*");
    }

    [Fact]
    public void WithAccountKey_CalledTwice_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountKey("dGVzdGtleQ==");

        // Act
        Action act = () => builder.WithAccountKey("YW5vdGhlcmtleQ==");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already been set*");
    }

    #endregion

    #region WithBlobEndpoint Method Tests

    [Fact]
    public void WithBlobEndpoint_WithValidHttpUri_ShouldSetBlobEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();
        var endpoint = "http://localhost:10000/testaccount";

        // Act
        var result = builder.WithBlobEndpoint(endpoint);

        // Assert
        result.Should().BeSameAs(builder);
        builder._properties["BlobEndpoint"].Should().Be(endpoint);
    }

    [Fact]
    public void WithBlobEndpoint_WithValidHttpsUri_ShouldSetBlobEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();
        var endpoint = "https://localhost:10000/testaccount";

        // Act
        var result = builder.WithBlobEndpoint(endpoint);

        // Assert
        builder._properties["BlobEndpoint"].Should().Be(endpoint);
    }

    [Fact]
    public void WithBlobEndpoint_WithNull_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithBlobEndpoint(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithBlobEndpoint_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithBlobEndpoint("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithBlobEndpoint_WithInvalidUri_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithBlobEndpoint("not a valid uri");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*valid URI*");
    }

    [Fact]
    public void WithBlobEndpoint_WithFtpProtocol_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithBlobEndpoint("ftp://localhost:10000/testaccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*http or https protocol*");
    }

    [Fact]
    public void WithBlobEndpoint_CalledTwice_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithBlobEndpoint("http://localhost:10000/testaccount");

        // Act
        Action act = () => builder.WithBlobEndpoint("http://localhost:10000/otheraccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already been set*");
    }

    #endregion

    #region WithQueueEndpoint Method Tests

    [Fact]
    public void WithQueueEndpoint_WithValidHttpUri_ShouldSetQueueEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();
        var endpoint = "http://localhost:10001/testaccount";

        // Act
        var result = builder.WithQueueEndpoint(endpoint);

        // Assert
        result.Should().BeSameAs(builder);
        builder._properties["QueueEndpoint"].Should().Be(endpoint);
    }

    [Fact]
    public void WithQueueEndpoint_WithValidHttpsUri_ShouldSetQueueEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();
        var endpoint = "https://localhost:10001/testaccount";

        // Act
        var result = builder.WithQueueEndpoint(endpoint);

        // Assert
        builder._properties["QueueEndpoint"].Should().Be(endpoint);
    }

    [Fact]
    public void WithQueueEndpoint_WithNull_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithQueueEndpoint(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithQueueEndpoint_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithQueueEndpoint("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithQueueEndpoint_WithInvalidUri_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithQueueEndpoint("not a valid uri");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*valid URI*");
    }

    [Fact]
    public void WithQueueEndpoint_WithFtpProtocol_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithQueueEndpoint("ftp://localhost:10001/testaccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*http or https protocol*");
    }

    [Fact]
    public void WithQueueEndpoint_CalledTwice_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithQueueEndpoint("http://localhost:10001/testaccount");

        // Act
        Action act = () => builder.WithQueueEndpoint("http://localhost:10001/otheraccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already been set*");
    }

    #endregion

    #region WithTableEndpoint Method Tests

    [Fact]
    public void WithTableEndpoint_WithValidHttpUri_ShouldSetTableEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();
        var endpoint = "http://localhost:10002/testaccount";

        // Act
        var result = builder.WithTableEndpoint(endpoint);

        // Assert
        result.Should().BeSameAs(builder);
        builder._properties["TableEndpoint"].Should().Be(endpoint);
    }

    [Fact]
    public void WithTableEndpoint_WithValidHttpsUri_ShouldSetTableEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();
        var endpoint = "https://localhost:10002/testaccount";

        // Act
        var result = builder.WithTableEndpoint(endpoint);

        // Assert
        builder._properties["TableEndpoint"].Should().Be(endpoint);
    }

    [Fact]
    public void WithTableEndpoint_WithNull_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithTableEndpoint(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithTableEndpoint_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithTableEndpoint("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithTableEndpoint_WithInvalidUri_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithTableEndpoint("not a valid uri");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*valid URI*");
    }

    [Fact]
    public void WithTableEndpoint_WithFtpProtocol_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder();

        // Act
        Action act = () => builder.WithTableEndpoint("ftp://localhost:10002/testaccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*http or https protocol*");
    }

    [Fact]
    public void WithTableEndpoint_CalledTwice_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithTableEndpoint("http://localhost:10002/testaccount");

        // Act
        Action act = () => builder.WithTableEndpoint("http://localhost:10002/otheraccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already been set*");
    }

    #endregion

    #region ToString Method Tests

    [Fact]
    public void ToString_WithAllRequiredProperties_ShouldReturnValidConnectionString()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("http://localhost:10000/testaccount");

        // Act
        var result = builder.ToString();

        // Assert
        result.Should().Contain("DefaultEndpointsProtocol=http");
        result.Should().Contain("AccountName=testaccount");
        result.Should().Contain("AccountKey=dGVzdGtleQ==");
        result.Should().Contain("BlobEndpoint=http://localhost:10000/testaccount");
    }

    [Fact]
    public void ToString_WithAllProperties_ShouldReturnValidConnectionString()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithProtocol("https")
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("https://localhost:10000/testaccount")
            .WithQueueEndpoint("https://localhost:10001/testaccount")
            .WithTableEndpoint("https://localhost:10002/testaccount");

        // Act
        var result = builder.ToString();

        // Assert
        result.Should().Contain("DefaultEndpointsProtocol=https");
        result.Should().Contain("AccountName=testaccount");
        result.Should().Contain("AccountKey=dGVzdGtleQ==");
        result.Should().Contain("BlobEndpoint=https://localhost:10000/testaccount");
        result.Should().Contain("QueueEndpoint=https://localhost:10001/testaccount");
        result.Should().Contain("TableEndpoint=https://localhost:10002/testaccount");
    }

    [Fact]
    public void ToString_WithoutDefaultEndpointsProtocol_ShouldDeriveFromBlobEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("https://localhost:10000/testaccount");

        // Act
        var result = builder.ToString();

        // Assert
        result.Should().Contain("DefaultEndpointsProtocol=https");
    }

    [Fact]
    public void ToString_WithoutAccountName_ShouldThrowFormatException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("http://localhost:10000/testaccount");

        // Act
        Action act = () => builder.ToString();

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*missing required properties*");
    }

    [Fact]
    public void ToString_WithoutAccountKey_ShouldThrowFormatException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount")
            .WithBlobEndpoint("http://localhost:10000/testaccount");

        // Act
        Action act = () => builder.ToString();

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*missing required properties*");
    }

    [Fact]
    public void ToString_WithoutBlobEndpoint_ShouldThrowFormatException()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==");

        // Act
        Action act = () => builder.ToString();

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*missing required properties*");
    }

    [Fact]
    public void ToString_WithOptionalQueueEndpoint_ShouldIncludeQueueEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("http://localhost:10000/testaccount")
            .WithQueueEndpoint("http://localhost:10001/testaccount");

        // Act
        var result = builder.ToString();

        // Assert
        result.Should().Contain("QueueEndpoint=http://localhost:10001/testaccount");
    }

    [Fact]
    public void ToString_WithoutOptionalQueueEndpoint_ShouldNotIncludeQueueEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("http://localhost:10000/testaccount");

        // Act
        var result = builder.ToString();

        // Assert
        result.Should().NotContain("QueueEndpoint");
    }

    [Fact]
    public void ToString_WithOptionalTableEndpoint_ShouldIncludeTableEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("http://localhost:10000/testaccount")
            .WithTableEndpoint("http://localhost:10002/testaccount");

        // Act
        var result = builder.ToString();

        // Assert
        result.Should().Contain("TableEndpoint=http://localhost:10002/testaccount");
    }

    [Fact]
    public void ToString_WithoutOptionalTableEndpoint_ShouldNotIncludeTableEndpoint()
    {
        // Arrange
        var builder = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("http://localhost:10000/testaccount");

        // Act
        var result = builder.ToString();

        // Assert
        result.Should().NotContain("TableEndpoint");
    }

    #endregion

    #region Fluent API / Chaining Tests

    [Fact]
    public void FluentAPI_ShouldAllowMethodChaining()
    {
        // Arrange & Act
        var builder = new AzuriteConnectionStringBuilder()
            .WithProtocol("http")
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("http://localhost:10000/testaccount")
            .WithQueueEndpoint("http://localhost:10001/testaccount")
            .WithTableEndpoint("http://localhost:10002/testaccount");

        // Assert
        builder.Should().NotBeNull();
        builder._properties.Should().HaveCount(6);
    }

    [Fact]
    public void ParseAndModify_ShouldThrowWhenAttemptingToSetExistingProperty()
    {
        // Arrange
        var connectionString = "DefaultEndpointsProtocol=http;AccountName=testaccount;AccountKey=dGVzdGtleQ==;BlobEndpoint=http://localhost:10000/testaccount";
        var builder = AzuriteConnectionStringBuilder.Parse(connectionString);

        // Act
        Action act = () => builder.WithAccountName("newaccount");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already been set*");
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void RoundTrip_ParseAndToString_ShouldProduceSameValues()
    {
        // Arrange
        var originalConnectionString = "DefaultEndpointsProtocol=http;AccountName=testaccount;AccountKey=dGVzdGtleQ==;BlobEndpoint=http://localhost:10000/testaccount";

        // Act
        var builder = AzuriteConnectionStringBuilder.Parse(originalConnectionString);
        var result = builder.ToString();

        // Assert
        result.Should().Contain("DefaultEndpointsProtocol=http");
        result.Should().Contain("AccountName=testaccount");
        result.Should().Contain("AccountKey=dGVzdGtleQ==");
        result.Should().Contain("BlobEndpoint=http://localhost:10000/testaccount");
    }

    [Fact]
    public void RoundTrip_BuildAndParse_ShouldProduceSameValues()
    {
        // Arrange
        var builder1 = new AzuriteConnectionStringBuilder()
            .WithAccountName("testaccount")
            .WithAccountKey("dGVzdGtleQ==")
            .WithBlobEndpoint("http://localhost:10000/testaccount");

        // Act
        var connectionString = builder1.ToString();
        var builder2 = AzuriteConnectionStringBuilder.Parse(connectionString);

        // Assert
        builder2._properties["AccountName"].Should().Be("testaccount");
        builder2._properties["AccountKey"].Should().Be("dGVzdGtleQ==");
        builder2._properties["BlobEndpoint"].Should().Be("http://localhost:10000/testaccount");
    }

    #endregion
}
