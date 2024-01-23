using Method4.UmbracoMigrator.Target.Core.Extensions;
using Method4.UmbracoMigrator.Target.Core.Hubs;
using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StackExchange.Profiling.Internal;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps
{
    public class MigrationPhase1 : IMigrationPhase1
    {
        private readonly IMappingCollectionService _migrationMapperService;
        private readonly IInternalDocTypeMapping _defaultDocTypeMapper;
        private readonly IInternalMediaTypeMapping _defaultMediaTypeMapper;
        private readonly IContentService _contentService;
        private readonly IMediaService _mediaService;
        private readonly IRelationLookupService _relationLookupService;
        private readonly ILogger<MigrationPhase1> _logger;
        private readonly HubService _hubService;

        private List<MigrationContent>? _contentNodesToMigrate;
        private List<MigrationMedia>? _mediaNodesToMigrate;
        private ImportSettings? _settings;

        public MigrationPhase1(IMappingCollectionService migrationMapperService,
            IInternalDocTypeMapping defaultDocTypeMapper,
            IInternalMediaTypeMapping defaultMediaTypeMapper,
            IContentService contentService, IMediaService mediaService,
            IRelationLookupService relationLookupService,
            ILogger<MigrationPhase1> logger,
            IHubContext<MigrationHub> hubContext)
        {
            _migrationMapperService = migrationMapperService;
            _defaultDocTypeMapper = defaultDocTypeMapper;
            _defaultMediaTypeMapper = defaultMediaTypeMapper;
            _contentService = contentService;
            _mediaService = mediaService;
            _relationLookupService = relationLookupService;
            _logger = logger;

            _contentNodesToMigrate = null;
            _mediaNodesToMigrate = null;
            _settings = null;

            _hubService = new HubService(hubContext);
        }

        public void SetupMigrationPhase(List<MigrationContent> contentNodesToMigrate, List<MigrationMedia> mediaNodesToMigrate, ImportSettings settings)
        {
            _contentNodesToMigrate = contentNodesToMigrate;
            _mediaNodesToMigrate = mediaNodesToMigrate;
            _settings = settings;
        }

        public void RunMigrationPhase()
        {
#pragma warning disable CS8604 // Possible null reference argument.
            _hubService.SendMessage(1, "Starting Migration Phase 1");
            _logger.LogInformation("Starting Migration Phase 1");
            CreateContentNodeStructure(_contentNodesToMigrate);
            CreateMediaNodeStructure(_mediaNodesToMigrate);
            _hubService.SendMessage(1, "Completed Migration Phase 1");
            _logger.LogInformation("Completed Migration Phase 1");
#pragma warning restore CS8604 // Possible null reference argument.
        }

        private void CreateContentNodeStructure(List<MigrationContent> nodesToMigrate)
        {
            var failedDocTypes = new List<string>();
            nodesToMigrate = nodesToMigrate.OrderBy(x => x.Level).ToList();
            var count = 0;

            foreach (var oldNode in nodesToMigrate)
            {
                count++;
                _hubService.SendMessage(1, $"Creating content node - {count}/{nodesToMigrate.Count} - \"{oldNode.Name.Truncate(20)}\"");
                var customMapping = _migrationMapperService.GetDocTypeMapping(oldNode);

                // If we can't map this node, then we will not create it
                if (customMapping == null && _defaultDocTypeMapper.CanIMap(oldNode) == false)
                {
                    _hubService.SendMessage(1, $"Could not create - \"{oldNode.Name.Truncate(20)}\" - No mapper exists for \"{oldNode.ContentType}\"");
                    _logger.LogWarning("No mapper exists for the \"{OldDocTypeAlias}\" DocType", oldNode.ContentType);
                    if (failedDocTypes.Contains(oldNode.ContentType) == false)
                    {
                        failedDocTypes.Add(oldNode.ContentType);
                    }
                    continue;
                }

                // Check if node already exists (if so, skip)
                if (DoesNodeAlreadyExist(oldNode.Key)) { continue; }

                // Create new node
                var contentType = customMapping?.DocTypeAlias ?? oldNode.ContentType;
                IContent newNode;

                if (oldNode.Level > 1)
                {
                    // As we have sorted by level, the parent should already have been created and it's new key logged
                    var relation = _relationLookupService.GetRelationByOldKey(oldNode.ParentKey);
                    var newParentKey = relation?.NewKeyAsGuid;
                    if (newParentKey == null)
                    {
                        _logger.LogError("Lookup for key '{parentKey}' Not found", oldNode.ParentKey);
                        throw new Exception($"Lookup for key '{oldNode.ParentKey}' Not found");
                    }
                    newNode = _defaultDocTypeMapper.CreateNode(oldNode, contentType, (Guid)newParentKey);
                }
                else
                {
                    newNode = _defaultDocTypeMapper.CreateRootNode(oldNode, contentType);
                }

                // Save
                try
                {
                    _contentService.Save(newNode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save node {nodeName}. old node key: {oldNodeKey}", newNode.Name, oldNode.Key);
                    throw;
                }

                // Store new key lookup
                _relationLookupService.StoreNewRelation(
                    newNode.Id.ToString(),
                    oldNode.Id,
                    newNode.Key,
                    oldNode.Key);
            }

            if (failedDocTypes.Any())
            {
                _logger.LogWarning("Failed to map the following DocTypes as no mappers exist for them: \"{OldDocTypeAliases}\"", string.Join(',', failedDocTypes));
            }
        }

        private void CreateMediaNodeStructure(List<MigrationMedia> nodesToMigrate)
        {
            nodesToMigrate = nodesToMigrate.OrderBy(x => x.Level).ToList();
            var count = 0;

            foreach (var oldNode in nodesToMigrate)
            {
                count++;
                _hubService.SendMessage(1, $"Creating media node - {count}/{nodesToMigrate.Count} - \"{oldNode.Name.Truncate(20)}\"");
                var customMapping = _migrationMapperService.GetMediaTypeMapping(oldNode);

                // If we can't map this node, then we will not create it
                if (customMapping == null && _defaultMediaTypeMapper.CanIMap(oldNode) == false) { continue; }

                // Check if node already exists (if so, skip)
                if (DoesNodeAlreadyExist(oldNode.Key)) { continue; }

                // Create new node
                var contentType = customMapping?.MediaTypeAlias ?? oldNode.ContentType;
                IMedia newNode;

                if (oldNode.Level > 1)
                {
                    // As we have sorted by level, the parent should already have been created and it's new key logged
                    var relation = _relationLookupService.GetRelationByOldKey(oldNode.ParentKey);
                    var newParentKey = relation?.NewKeyAsGuid;
                    if (newParentKey == null)
                    {
                        _logger.LogError("Lookup for key '{parentKey}' Not found", oldNode.ParentKey);
                        throw new Exception($"Lookup for key '{oldNode.ParentKey}' Not found");
                    }
                    newNode = _defaultMediaTypeMapper.CreateNode(oldNode, contentType, (Guid)newParentKey);
                }
                else
                {
                    newNode = _defaultMediaTypeMapper.CreateRootNode(oldNode, contentType);
                }

                // Save
                try
                {
                    _mediaService.Save(newNode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save node {nodeName}. old node key: {oldNodeKey}", newNode.Name, oldNode.Key);
                    throw;
                }

                // Store new key lookup
                _relationLookupService.StoreNewRelation(
                    newNode.Id.ToString(),
                    oldNode.Id,
                    newNode.Key,
                    oldNode.Key);
            }
        }

        private bool DoesNodeAlreadyExist(Guid oldKey)
        {
            // Do we have a key lookup entry?
            var relation = _relationLookupService.GetRelationByOldKey(oldKey);
            var newKey = relation?.NewKeyAsGuid;
            if (newKey == null) { return false; }

            // Does a content node with the key exist?
            var contentNode = _contentService.GetById((Guid)newKey);
            if (contentNode != null)
            {
                return true;
            }

            // Does a media node with the key exist?
            var mediaNode = _mediaService.GetById((Guid)newKey);
            if (mediaNode != null)
            {
                return true;
            }

            return false;
        }
    }
}