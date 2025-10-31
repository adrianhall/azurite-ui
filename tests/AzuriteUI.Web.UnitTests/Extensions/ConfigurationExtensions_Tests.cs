using AzuriteUI.Web.Extensions;
using AzuriteUI.Web.UnitTests.Helpers;
using Microsoft.Extensions.Configuration;

namespace AzuriteUI.Web.UnitTests.Extensions;

[ExcludeFromCodeCoverage]
public class ConfigurationExtensions_Tests
{
    #region GetRequiredConnectionString Tests

    [Fact(Timeout = 15000)]
    public void GetRequiredConnectionString_WithValidConnectionString_ShouldReturnConnectionString()
    {
        // Arrange
        var connectionStringName = "TestConnection";
        var expectedConnectionString = "Server=localhost;Database=test;";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionStringName}"] = expectedConnectionString
        });

        // Act
        var result = configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        result.Should().Be(expectedConnectionString);
    }

    [Fact(Timeout = 15000)]
    public void GetRequiredConnectionString_WithNullConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var connectionStringName = "TestConnection";
        var configuration = Utils.CreateConfiguration();

        // Act
        Action act = () => configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Connection string '{connectionStringName}' is not configured.");
    }

    [Fact(Timeout = 15000)]
    public void GetRequiredConnectionString_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var connectionStringName = "TestConnection";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionStringName}"] = ""
        });

        // Act
        Action act = () => configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Connection string '{connectionStringName}' is not configured.");
    }

    [Fact(Timeout = 15000)]
    public void GetRequiredConnectionString_WithWhitespaceConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var connectionStringName = "TestConnection";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionStringName}"] = "   "
        });

        // Act
        Action act = () => configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Connection string '{connectionStringName}' is not configured.");
    }

    [Fact(Timeout = 15000)]
    public void GetRequiredConnectionString_WithDifferentConnectionStringName_ShouldIncludeNameInExceptionMessage()
    {
        // Arrange
        var connectionStringName = "AzuriteConnection";
        var configuration = Utils.CreateConfiguration();

        // Act
        Action act = () => configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Connection string '{connectionStringName}' is not configured.");
    }

    [Fact(Timeout = 15000)]
    public void GetRequiredConnectionString_WithComplexConnectionString_ShouldReturnConnectionString()
    {
        // Arrange
        var connectionStringName = "Azurite";
        var expectedConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionStringName}"] = expectedConnectionString
        });

        // Act
        var result = configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        result.Should().Be(expectedConnectionString);
    }

    #endregion

    #region GetTimeSpan Tests

    [Fact(Timeout = 15000)]
    public void GetTimeSpan_WithValidTimeSpanValue_ShouldReturnTimeSpan()
    {
        // Arrange
        var key = "TestTimeSpan";
        var timeSpanString = "00:05:30";
        var expectedTimeSpan = TimeSpan.Parse(timeSpanString);
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [key] = timeSpanString
        });

        // Act
        var result = configuration.GetTimeSpan(key);

        // Assert
        result.Should().Be(expectedTimeSpan);
    }

    [Fact(Timeout = 15000)]
    public void GetTimeSpan_WithDaysHoursMinutesSeconds_ShouldReturnTimeSpan()
    {
        // Arrange
        var key = "TestTimeSpan";
        var timeSpanString = "1.02:03:04";
        var expectedTimeSpan = TimeSpan.Parse(timeSpanString);
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [key] = timeSpanString
        });

        // Act
        var result = configuration.GetTimeSpan(key);

        // Assert
        result.Should().Be(expectedTimeSpan);
    }

    [Fact(Timeout = 15000)]
    public void GetTimeSpan_WithInvalidTimeSpanValue_ShouldReturnNull()
    {
        // Arrange
        var key = "TestTimeSpan";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [key] = "invalid-timespan"
        });

        // Act
        var result = configuration.GetTimeSpan(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void GetTimeSpan_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var key = "TestTimeSpan";
        var configuration = Utils.CreateConfiguration();

        // Act
        var result = configuration.GetTimeSpan(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void GetTimeSpan_WithEmptyString_ShouldReturnNull()
    {
        // Arrange
        var key = "TestTimeSpan";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [key] = ""
        });

        // Act
        var result = configuration.GetTimeSpan(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void GetTimeSpan_WithWhitespaceString_ShouldReturnNull()
    {
        // Arrange
        var key = "TestTimeSpan";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [key] = "   "
        });

        // Act
        var result = configuration.GetTimeSpan(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void GetTimeSpan_WithZeroTimeSpan_ShouldReturnZero()
    {
        // Arrange
        var key = "TestTimeSpan";
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [key] = "00:00:00"
        });

        // Act
        var result = configuration.GetTimeSpan(key);

        // Assert
        result.Should().Be(TimeSpan.Zero);
    }

    [Fact(Timeout = 15000)]
    public void GetTimeSpan_WithNegativeTimeSpan_ShouldReturnNegativeTimeSpan()
    {
        // Arrange
        var key = "TestTimeSpan";
        var timeSpanString = "-01:30:00";
        var expectedTimeSpan = TimeSpan.Parse(timeSpanString);
        var configuration = Utils.CreateConfiguration(new Dictionary<string, string?>
        {
            [key] = timeSpanString
        });

        // Act
        var result = configuration.GetTimeSpan(key);

        // Assert
        result.Should().Be(expectedTimeSpan);
    }

    #endregion
}
