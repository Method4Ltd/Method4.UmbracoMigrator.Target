using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Method4.UmbracoMigrator.Target.Core.Services
{
    public class MigratorBlobService : IMigratorBlobService
    {
        private readonly ILogger<MigratorBlobService> _logger;

        private readonly string? _blobConnectionString;
        private readonly string? _blobContainerName;

        public MigratorBlobService(ILogger<MigratorBlobService> logger, IConfiguration config)
        {
            _logger = logger;

            _blobConnectionString = config.GetValue<string>("Umbraco:Storage:AzureBlob:Media:ConnectionString") ?? null;
            _blobContainerName = config.GetValue<string>("Umbraco:Storage:AzureBlob:Media:ContainerName") ?? null;
        }

        /// <summary>
        /// Get the BlobContainerClient object
        /// </summary>
        /// <returns></returns>
        public BlobContainerClient GetBlobContainerClient()
        {
            _logger.LogInformation("Connecting to blob container {containerName}", _blobContainerName);
            var blobServiceClient = new BlobServiceClient(_blobConnectionString);
            return blobServiceClient.GetBlobContainerClient(_blobContainerName);
        }
    }
}