using Method4.UmbracoMigrator.Target.Core.Extensions;
using Method4.UmbracoMigrator.Target.Core.Hubs;
using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps
{
    internal class MigrationPhase3 : IMigrationPhase3
    {
        private readonly IMappingCollectionService _migrationMapperService;
        private readonly IInternalDocTypeMapping _defaultDocTypeMapper;
        private readonly IContentService _contentService;
        private readonly ILogger<MigrationPhase3> _logger;
        private readonly IRelationLookupService _relationLookupService;
        private readonly HubService _hubService;

        private List<MigrationContent>? _contentNodesToMigrate;
        private ImportSettings? _settings;

        public MigrationPhase3(IMappingCollectionService migrationMapperService,
            IInternalDocTypeMapping defaultDocTypeMapper,
            IContentService contentService,
            ILogger<MigrationPhase3> logger,
            IRelationLookupService relationLookupService,
            IHubContext<MigrationHub> hubContext)
        {
            _migrationMapperService = migrationMapperService;
            _defaultDocTypeMapper = defaultDocTypeMapper;
            _contentService = contentService;
            _logger = logger;
            _relationLookupService = relationLookupService;

            _contentNodesToMigrate = null;
            _settings = null;

            _hubService = new HubService(hubContext);
        }

        public void SetupMigrationPhase(List<MigrationContent> contentNodesToMigrate, ImportSettings settings)
        {
            _contentNodesToMigrate = contentNodesToMigrate;
            _settings = settings;
        }

        public void RunMigrationPhase()
        {
#pragma warning disable CS8604 // Possible null reference argument.
            _hubService.SendMessage(3, "Starting Migration Phase 3");
            _logger.LogInformation("Starting Migration Phase 3");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            MigrateContentProperties(_contentNodesToMigrate);

            stopwatch.Stop();

            _hubService.SendMessage(3, $"Completed Migration Phase 3 - {stopwatch.Humanize()}");
            _logger.LogInformation("Completed Migration Phase 3 - {elapsedMilliseconds}", stopwatch.Humanize());
#pragma warning restore CS8604 // Possible null reference argument.
        }

        private void MigrateContentProperties(List<MigrationContent> nodesToMigrate)
        {
            _logger.LogInformation("Starting Migration of Content property values");
            nodesToMigrate = nodesToMigrate.OrderBy(x => x.Level).ToList();
            var count = 0;

            foreach (var oldNode in nodesToMigrate)
            {
                count++;
                _hubService.SendMessage(3, $"Migrating content node properties - {count}/{nodesToMigrate.Count} - \"{oldNode.Name.Truncate(20)}\"");
                var customMapping = _migrationMapperService.GetDocTypeMapping(oldNode);

                // Find our new node
                var relation = _relationLookupService.GetRelationByOldKey(oldNode.Key);
                var newKey = relation?.NewKeyAsGuid;
                if (newKey == null)
                {
                    _logger.LogError("Could not find key lookup for {oldKey}", oldNode.Key);
                    continue;
                }

                var newNode = _contentService.GetById((Guid)newKey);
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
                        newNode = _defaultDocTypeMapper.MapNode(oldNode, newNode, _settings.OverwriteExistingValues);
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
                        _logger.LogError(
                            ex,
                            "Failed to map properties from old node [{oldNodeId}, {oldNodeKey}] to new node [{newNodeId}, {newNodeKey}], using custom mapper '{className}'",
                            oldNode.Id, oldNode.Key, newNode.Id, newNode.Key, customMappingType.Name);
                        throw new Exception($"Failed to map properties from old node [{oldNode.Id}, {oldNode.Key}] to new node [{newNode.Id}, {newNode.Key}], using custom mapper '{customMappingType.Name}'",
                            ex);
                    }
                }

                // Save
                try
                {
                    _contentService.Save(newNode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save content node {nodeId} [{nodeKey}], after mapping property values.", newNode.Id, newNode.Key);
                    throw;
                }
            }
            _logger.LogInformation("Completed Migration of Content property values");
        }
    }
}
