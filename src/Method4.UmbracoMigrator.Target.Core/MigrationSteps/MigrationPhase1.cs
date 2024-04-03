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
using Umbraco.Cms.Core.Strings;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps
{
    public class MigrationPhase1 : IMigrationPhase1
    {
        private readonly IMappingCollectionService _migrationMapperService;
        private readonly IInternalDocTypeMapping _defaultDocTypeMapper;
        private readonly IInternalMediaTypeMapping _defaultMediaTypeMapper;
        private readonly IContentTypeService _contentTypeService;
        private readonly IMediaTypeService _mediaTypeService;
        private readonly IContentService _contentService;
        private readonly IMediaService _mediaService;
        private readonly IRelationLookupService _relationLookupService;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly ILogger<MigrationPhase1> _logger;
        private readonly HubService _hubService;

        private List<MigrationContent>? _contentNodesToMigrate;
        private List<MigrationMedia>? _mediaNodesToMigrate;
        private ImportSettings? _settings;

        public MigrationPhase1(IMappingCollectionService migrationMapperService,
            IInternalDocTypeMapping defaultDocTypeMapper,
            IInternalMediaTypeMapping defaultMediaTypeMapper,
            IContentTypeService contentTypeService, IMediaTypeService mediaTypeService,
            IContentService contentService, IMediaService mediaService,
            IRelationLookupService relationLookupService,
            IShortStringHelper shortStringHelper,
            ILogger<MigrationPhase1> logger,
            IHubContext<MigrationHub> hubContext)
        {
            _migrationMapperService = migrationMapperService;
            _defaultDocTypeMapper = defaultDocTypeMapper;
            _defaultMediaTypeMapper = defaultMediaTypeMapper;
            _contentTypeService = contentTypeService;
            _mediaTypeService = mediaTypeService;
            _contentService = contentService;
            _mediaService = mediaService;
            _relationLookupService = relationLookupService;
            _shortStringHelper = shortStringHelper;
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
            MoveTempNodes();
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
                    if (oldNode.Trashed && oldNode.ParentKey.ToString() == "916724a5-173d-4619-b97e-b9de133dd6f5")
                    {
                        // 916724a5-173d-4619-b97e-b9de133dd6f5 is the GUID of the Umbraco Master Root - https://github.com/umbraco/Umbraco-CMS/blob/d8b0616434d1114d0628fb5702206bea39baa88b/src/Umbraco.Infrastructure/Migrations/Install/DatabaseDataCreator.cs#L178
                        // Trashed items can have this as it's parent key
                        newNode = _defaultDocTypeMapper.CreateRootNode(oldNode, contentType);
                    }
                    else
                    {
                        var parentRelation = _relationLookupService.GetRelationByOldKey(oldNode.ParentKey);
                        if (parentRelation == null)
                        {
                            // Add as a child to the temp node, if we haven't yet migrated it's parent
                            _logger.LogWarning("Lookup for key '{parentKey}' Not found, '{oldKey}' will be added to the temp node.", oldNode.ParentKey, oldNode.Key);
                            newNode = AddToTempParentNode(oldNode, contentType);
                        }
                        else
                        {
                            newNode = _defaultDocTypeMapper.CreateNode(oldNode, contentType, parentRelation.NewKeyAsGuid);
                        }
                    }
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
                    if (oldNode.Trashed && oldNode.ParentKey.ToString() == "916724a5-173d-4619-b97e-b9de133dd6f5")
                    {
                        // 916724a5-173d-4619-b97e-b9de133dd6f5 is the GUID of the Umbraco Master Root - https://github.com/umbraco/Umbraco-CMS/blob/d8b0616434d1114d0628fb5702206bea39baa88b/src/Umbraco.Infrastructure/Migrations/Install/DatabaseDataCreator.cs#L178
                        // Trashed items can have this as it's parent key
                        newNode = _defaultMediaTypeMapper.CreateRootNode(oldNode, contentType);
                    }
                    else
                    {
                        var parentRelation = _relationLookupService.GetRelationByOldKey(oldNode.ParentKey);
                        if (parentRelation == null)
                        {

                            // Add as a child to the temp node, if we haven't yet migrated it's parent
                            _logger.LogWarning("Lookup for key '{parentKey}' Not found, '{oldKey}' will be added to the temp node.", oldNode.ParentKey, oldNode.Key);
                            newNode = AddToTempParentNode(oldNode, contentType);
                        }
                        else
                        {
                            newNode = _defaultMediaTypeMapper.CreateNode(oldNode, contentType, parentRelation.NewKeyAsGuid);
                        }
                    }
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

        private void MoveTempNodes()
        {
            // Move content nodes from the temp node, to their migrated parent nodes
            var tempContentParent = _contentService.GetByLevel(1).FirstOrDefault(c => c.ContentType.Alias == "migrationTempParentNode");
            if (tempContentParent != null)
            {
                var nodes = _contentService.GetPagedChildren(tempContentParent.Id, 0, int.MaxValue, out var totalBeforeMoving);
                foreach (var node in nodes)
                {
                    // Get the migration relation for this node
                    var relation = _relationLookupService.GetRelationByNewId(node.Id.ToString());
                    if (relation == null) { continue; }

                    // Find the migration data for this node
                    var migrationNode = _contentNodesToMigrate?.FirstOrDefault(x => x.Id == relation.OldId);
                    if (migrationNode == null) { continue; }

                    // Get the migration relation for it's parent
                    var parentRelation = _relationLookupService.GetRelationByOldKey(migrationNode.ParentKey);
                    if (parentRelation == null) { continue; }

                    // Find it's migrated parent and move it
                    var newParent = _contentService.GetById(parentRelation.NewKeyAsGuid);
                    if (newParent != null)
                    {
                        _hubService.SendMessage(1, $"Moving {node.Id} from temp node to it's actual parent {newParent.Id}");
                        _logger.LogInformation("Moving {newId} from temp node to it's actual parent {parentId}", node.Id, newParent.Id);
                        _contentService.Move(node, newParent.Id);
                    }
                }

                // Delete the temp node
                _contentService.GetPagedChildren(tempContentParent.Id, 0, int.MaxValue, out var totalAfterMoving);
                if (totalAfterMoving == 0)
                {
                    _contentService.Delete(tempContentParent);
                }
            }

            // Move media nodes from the temp node, to their migrated parent nodes
            var tempMediaParent = _mediaService.GetByLevel(1)?.FirstOrDefault(c => c.ContentType.Alias == "migrationTempParentNode");
            if (tempMediaParent != null)
            {
                var nodes = _mediaService.GetPagedChildren(tempMediaParent.Id, 0, int.MaxValue, out var totalBeforeMoving);
                foreach (var node in nodes)
                {
                    // Get the migration relation for this node
                    var relation = _relationLookupService.GetRelationByNewId(node.Id.ToString());
                    if (relation == null) { continue; }

                    // Find the migration data for this node
                    var migrationNode = _mediaNodesToMigrate?.FirstOrDefault(x => x.Id == relation.OldId);
                    if (migrationNode == null) { continue; }

                    // Get the migration relation for it's parent
                    var parentRelation = _relationLookupService.GetRelationByOldKey(migrationNode.ParentKey);
                    if (parentRelation == null) { continue; }

                    // Find it's migrated parent and move it
                    var newParent = _mediaService.GetById(parentRelation.NewKeyAsGuid);
                    if (newParent != null)
                    {
                        _hubService.SendMessage(1, $"Moving {node.Id} from temp node to it's actual parent {newParent.Id}");
                        _logger.LogInformation("Moving {newId} from temp node to it's actual parent {parentId}", node.Id, newParent.Id);
                        _mediaService.Move(node, newParent.Id);
                    }
                }

                // Delete the temp node
                _mediaService.GetPagedChildren(tempMediaParent.Id, 0, int.MaxValue, out var totalAfterMoving);
                if (totalAfterMoving == 0)
                {
                    _mediaService.Delete(tempMediaParent);
                }
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

        private IContent AddToTempParentNode(MigrationContent oldNode, string contentType)
        {
            _hubService.SendMessage(1, $"Saving '{oldNode.Key}' under temp node");
            _logger.LogWarning("Saving '{oldKey}' under temp node", oldNode.Key);
            var tempParent = GetOrCreateTempParentContentNode();
            return _defaultDocTypeMapper.CreateNode(oldNode, contentType, tempParent.Key);
        }

        private IMedia AddToTempParentNode(MigrationMedia oldNode, string contentType)
        {
            _hubService.SendMessage(1, $"Saving '{oldNode.Key}' under temp node");
            _logger.LogWarning("Saving '{oldKey}' under temp node", oldNode.Key);
            var tempParent = GetOrCreateTempParentMediaNode();
            return _defaultMediaTypeMapper.CreateNode(oldNode, contentType, tempParent.Key);
        }

        private IContent GetOrCreateTempParentContentNode()
        {
            var tempParent = _contentService.GetByLevel(1).FirstOrDefault(c => c.ContentType.Alias == "migrationTempParentNode");

            // Create the temp node if it doesn't exist
            if (tempParent == null)
            {
                // Create DocType if it doesn't exist
                var tempContentType = _contentTypeService.Get("migrationTempParentNode");
                if (tempContentType == null)
                {
                    tempContentType = new ContentType(_shortStringHelper, -1) { Alias = "migrationTempParentNode", Icon = "icon-truck color-deep-purple" };
                    tempContentType.Name = "Migration Temporary Parent Node";
                    _contentTypeService.Save(tempContentType);
                }

                tempParent = _contentService.CreateAndSave("Migration Temp Parent", -1, "migrationTempParentNode");
            }

            return tempParent;
        }

        private IMedia GetOrCreateTempParentMediaNode()
        {
            var tempParent = _mediaService.GetByLevel(1)?.FirstOrDefault(c => c.ContentType.Alias == "migrationTempParentNode");

            // Create the temp node if it doesn't exist
            if (tempParent == null)
            {
                // Create DocType if it doesn't exist
                var tempContentType = _mediaTypeService.Get("migrationTempParentNode");
                if (tempContentType == null)
                {
                    tempContentType = new MediaType(_shortStringHelper, -1) { Alias = "migrationTempParentNode", Icon = "icon-truck color-deep-purple" };
                    tempContentType.Name = "Migration Temporary Parent Node";
                    _mediaTypeService.Save(tempContentType);
                }

                tempParent = _mediaService.CreateMediaWithIdentity("Migration Temp Parent", -1, "migrationTempParentNode");
            }

            return tempParent;
        }
    }
}