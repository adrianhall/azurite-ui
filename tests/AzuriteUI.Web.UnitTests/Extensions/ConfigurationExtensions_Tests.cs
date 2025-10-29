using AzuriteUI.Web.Extensions;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace AzuriteUI.Web.UnitTests.Extensions;

[ExcludeFromCodeCoverage]
public class ConfigurationExtensions_Tests
{
    #region GetRequiredConnectionString Tests

    [Fact]
    public void GetRequiredConnectionString_WithValidConnectionString_ShouldReturnConnectionString()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        var connectionStringName = "TestConnection";
        var expectedConnectionString = "Server=localhost;Database=test;";
        configuration.GetConnectionString(connectionStringName).Returns(expectedConnectionString);

        // Act
        var result = configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        result.Should().Be(expectedConnectionString);
    }

    [Fact]
    public void GetRequiredConnectionString_WithNullConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        var connectionStringName = "TestConnection";
        configuration.GetConnectionString(connectionStringName).Returns((string?)null);

        // Act
        Action act = () => configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Connection string '{connectionStringName}' is not configured.");
    }

    [Fact]
    public void GetRequiredConnectionString_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        var connectionStringName = "TestConnection";
        configuration.GetConnectionString(connectionStringName).Returns("");

        // Act
        Action act = () => configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Connection string '{connectionStringName}' is not configured.");
    }

    [Fact]
    public void GetRequiredConnectionString_WithWhitespaceConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        var connectionStringName = "TestConnection";
        configuration.GetConnectionString(connectionStringName).Returns("   ");

        // Act
        Action act = () => configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Connection string '{connectionStringName}' is not configured.");
    }

    [Fact]
    public void GetRequiredConnectionString_WithDifferentConnectionStringName_ShouldIncludeNameInExceptionMessage()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        var connectionStringName = "AzuriteConnection";
        configuration.GetConnectionString(connectionStringName).Returns((string?)null);

        // Act
        Action act = () => configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Connection string '{connectionStringName}' is not configured.");
    }

    [Fact]
    public void GetRequiredConnectionString_WithComplexConnectionString_ShouldReturnConnectionString()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        var connectionStringName = "Azurite";
        var expectedConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
        configuration.GetConnectionString(connectionStringName).Returns(expectedConnectionString);

        // Act
        var result = configuration.GetRequiredConnectionString(connectionStringName);

        // Assert
        result.Should().Be(expectedConnectionString);
    }

    #endregion
}
