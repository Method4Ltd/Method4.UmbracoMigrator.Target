using Azure.Storage.Blobs;

namespace Method4.UmbracoMigrator.Target.Core.Services
{
    public interface IMigratorBlobService
    {
        BlobContainerClient GetBlobContainerClient();
    }
}