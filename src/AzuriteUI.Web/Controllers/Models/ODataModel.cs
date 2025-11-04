using AzuriteUI.Web.Services.Repositories.Models;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace AzuriteUI.Web.Controllers.Models;

/// <summary>
/// A set of methods for creating OData models.
/// </summary>
public static class ODataModel
{
    /// <summary>
    /// Creates the <see cref="IEdmModel"/> for OData controllers.
    /// </summary>
    /// <returns>The <see cref="IEdmModel"/> for OData controllers.</returns>
    public static IEdmModel BuildEdmModel()
    {
        ODataConventionModelBuilder odataBuilder = new();
        odataBuilder.EnableLowerCamelCase();

        // Configuration of the ContainerDTO entity
        var containerEntity = odataBuilder.EntitySet<ContainerDTO>("Containers").EntityType;
        containerEntity.HasKey(c => c.Name);
        containerEntity.Ignore(c => c.Metadata); // Ignore Metadata dictionary for OData $select

        // Configuration of the BlobDTO entity
        var blobEntity = odataBuilder.EntitySet<BlobDTO>("Blobs").EntityType;
        blobEntity.HasKey(b => new { b.ContainerName, b.Name });
        blobEntity.Ignore(b => b.Metadata); // Ignore Metadata dictionary for OData $select
        blobEntity.Ignore(b => b.Tags); // Ignore Tags dictionary for OData $select

        // Configuration of the UploadDTO entity
        var uploadEntity = odataBuilder.EntitySet<UploadDTO>("Uploads").EntityType;
        uploadEntity.HasKey(u => u.Id);

        return odataBuilder.GetEdmModel();
    }
}