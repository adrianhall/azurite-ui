using AzuriteUI.Web.Controllers.Models;

namespace AzuriteUI.Web.UnitTests.Controllers.Models;

[ExcludeFromCodeCoverage(Justification = "Test class")]
public class ODataModel_Tests
{
    [Fact(Timeout = 15000)]
    public async Task BuildEdmModel_ProducesAllSchemaElements()
    {
        // Arrange
        var model = ODataModel.BuildEdmModel();

        // Assert
        model.Should().NotBeNull();
        model.SchemaElements.Should().ContainSingle(e => e.Name == "ContainerDTO");
        model.SchemaElements.Should().ContainSingle(e => e.Name == "BlobDTO");
        model.SchemaElements.Should().ContainSingle(e => e.Name == "UploadDTO");
    }    
}