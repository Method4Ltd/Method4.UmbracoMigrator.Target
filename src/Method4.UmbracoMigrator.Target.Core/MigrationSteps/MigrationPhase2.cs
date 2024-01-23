using Method4.UmbracoMigrator.Source.Core.Services;
using Method4.UmbracoMigrator.Target.Core.Hubs;
using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps
{
    public class MigrationPhase2 : IMigrationPhase2
    {
        private readonly IMigratorFileService _migratorFileService;
        private readonly ILogger<MigrationPhase2> _logger;
        private readonly IMediaService _mediaService;
        private readonly IMappingCollectionService _migrationMapperService;
        private readonly IInternalMediaTypeMapping _defaultMediaTypeMapper;
        private readonly IRelationLookupService _relationLookupService;
        private readonly HubService _hubService;

        private List<MigrationMedia>? _mediaNodesToMigrate;
        private ImportSettings? _settings;
        private readonly bool isBlob;

        public MigrationPhase2(IMigratorFileService migratorFileService,
            ILogger<MigrationPhase2> logger,
            IConfiguration config,
            IMediaService mediaService,
            IMappingCollectionService migrationMapperService,
            IInternalMediaTypeMapping defaultMediaTypeMapper,
            IRelationLookupService relationLookupService,
            IHubContext<MigrationHub> hubContext)
        {
            _migratorFileService = migratorFileService;
            _logger = logger;
            _mediaService = mediaService;
            _migrationMapperService = migrationMapperService;
            _defaultMediaTypeMapper = defaultMediaTypeMapper;
            _relationLookupService = relationLookupService;

            var blobConnectionString = config.GetValue<string>("Umbraco:Storage:AzureBlob:Media:ConnectionString") ?? null;
            isBlob = !blobConnectionString.IsNullOrWhiteSpace();

            _mediaNodesToMigrate = null;
            _settings = null;

            _hubService = new HubService(hubContext);
        }

        public void SetupMigrationPhase(List<MigrationMedia> mediaNodesToMigrate, ImportSettings settings)
        {
            _mediaNodesToMigrate = mediaNodesToMigrate;
            _settings = settings;
        }

        public void RunMigrationPhase()
        {
#pragma warning disable CS8604 // Possible null reference argument.
            _hubService.SendMessage(2, "Starting Migration Phase 2");
            _logger.LogInformation("Starting Migration Phase 2");
            MigrateMediaFiles(_mediaNodesToMigrate);
            MigrateMediaProperties(_mediaNodesToMigrate);
            _hubService.SendMessage(2, "Completed Migration Phase 2");
            _logger.LogInformation("Completed Migration Phase 2");
#pragma warning restore CS8604 // Possible null reference argument.
        }

        private void MigrateMediaFiles(List<MigrationMedia> nodesToMigrate)
        {
            if (isBlob)
            {
                _hubService.SendMessage(2, "Copying media files to Blob Storage");
                _migratorFileService.MigrateMediaFilesToBlob();
            }
            else
            {
                _hubService.SendMessage(2, "Copying media files to media folder");
                _migratorFileService.MigrateMediaFilesToDisk();
            }
        }

        private void MigrateMediaProperties(List<MigrationMedia> nodesToMigrate)
        {
            _logger.LogInformation("Starting Migration of Media property values");
            nodesToMigrate = nodesToMigrate.OrderBy(x => x.Level).ToList();
            var count = 0;

            foreach (var oldNode in nodesToMigrate)
            {
                count++;
                _hubService.SendMessage(2, $"Migrating media node properties - {count}/{nodesToMigrate.Count} - \"{oldNode.Name.Truncate(20)}\"");
                var customMapping = _migrationMapperService.GetMediaTypeMapping(oldNode);

                // Find our new node
                var relation = _relationLookupService.GetRelationByOldKey(oldNode.Key);
                var newKey = relation?.NewKeyAsGuid;
                if (newKey == null)
                {
                    _logger.LogError("Could not find key lookup for {oldKey}", oldNode.Key);
                    continue;
                }

                var newNode = _mediaService.GetById((Guid)newKey);
                if (newNode == null)
                {
                    _logger.LogError("Could not find IMedia with guid {newKey}", newKey?.ToString());
                    throw new Exception($"Could not find IMedia with guid {newKey?.ToString()}");
                }

                // Run our default mapper
                if (_settings!.DisableDefaultMappers == false)
                {
                    try
                    {
                        newNode = _defaultMediaTypeMapper.MapNode(oldNode, newNode, _settings.OverwriteExistingValues);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to map properties from old node [{oldNodeId}, {oldNodeKey}] to new node [{newNodeId}, {newNodeKey}], using the default mapper",
                            oldNode.Id, oldNode.Key, newNode.Id, newNode.Key);
                        throw new Exception($"Failed to map properties from old node [{oldNode.Id}, {oldNode.Key}] to new node [{newNode.Id}, {newNode.Key}], using the default mapper",
                            ex);
                    }
                }

                // Run the Custom mapper
                if (customMapping != null)
                {
                    try
                    {
                        newNode = customMapping.MapNode(oldNode, newNode, _settings.OverwriteExistingValues);
                    }
                    catch (Exception ex)
                    {
                        var customMappingType = customMapping.GetType();
                        _logger.LogError(ex,
                            "Failed to map properties from old node [{oldNodeId}, {oldNodeKey}] to new node [{newNodeId}, {newNodeKey}], using custom mapper '{className}'",
                            oldNode.Id, oldNode.Key, newNode.Id, newNode.Key, customMappingType.Name);
                        throw new Exception($"Failed to map properties from old node [{oldNode.Id}, {oldNode.Key}] to new node [{newNode.Id}, {newNode.Key}], using custom mapper '{customMappingType.Name}'",
                            ex);
                    }
                }

                // Save
                try
                {
                    _mediaService.Save(newNode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to media save node {nodeId} [{nodeKey}], after mapping property values.", newNode.Id, newNode.Key);
                    throw;
                }
            }
            _logger.LogInformation("Completed Migration of Media property values");
        }
    }
}