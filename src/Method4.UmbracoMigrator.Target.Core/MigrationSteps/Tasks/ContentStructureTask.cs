using Method4.UmbracoMigrator.Target.Core.Extensions;
using Method4.UmbracoMigrator.Target.Core.Hubs;
using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StackExchange.Profiling.Internal;
using System.Diagnostics;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps.Tasks
{
    internal class ContentStructureTask(IMappingCollectionService migrationMapperService,
        IInternalDocTypeMapping defaultDocTypeMapper,
        IContentTypeService contentTypeService,
        IContentService contentService,
        IRelationLookupService relationLookupService,
        IShortStringHelper shortStringHelper,
        ILogger<ContentStructureTask> logger,
        IHubContext<MigrationHub> hubContext) : IContentStructureTask
    {
        private readonly HubService _hubService = new(hubContext);

        /// <inheritdoc />
        public void CreateContentNodeStructure(List<MigrationContent> nodesToMigrate)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            logger.LogInformation("Creating Content Structure");

            var failedDocTypes = new List<string>();
            nodesToMigrate = nodesToMigrate.OrderBy(x => x.Level).ToList();
            var count = 0;

            foreach (var oldNode in nodesToMigrate)
            {
                count++;
                var nodeStopwatch = new Stopwatch();
                nodeStopwatch.Start();

                var oldName = oldNode.Name.Truncate(20);

                _hubService.SendMessage(1, $"Creating content node - {count}/{nodesToMigrate.Count} - \"{oldName}\"");
                logger.LogDebug("Creating content node - {count}/{total} - \"{oldName}\"", count, nodesToMigrate.Count, oldName);

                var customMapping = migrationMapperService.GetDocTypeMapping(oldNode);

                // If we can't map this node, then we will not create it
                if (customMapping == null && defaultDocTypeMapper.CanIMap(oldNode) == false)
                {
                    _hubService.SendMessage(1, $"Could not create - \"{oldNode.Name.Truncate(20)}\" - No document type mapper exists for \"{oldNode.ContentType}\"");
                    logger.LogWarning("No mapper exists for the \"{OldDocTypeAlias}\" DocType", oldNode.ContentType);
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
                        newNode = defaultDocTypeMapper.CreateRootNode(oldNode, contentType);
                    }
                    else
                    {
                        var parentRelation = relationLookupService.GetRelationByOldKey(oldNode.ParentKey);
                        if (parentRelation == null)
                        {
                            // Add as a child to the temp node, if we haven't yet migrated it's parent
                            logger.LogWarning("Lookup for key '{parentKey}' Not found, '{oldKey}' will be added to the temp content node.", oldNode.ParentKey, oldNode.Key);
                            newNode = AddToTempParentNode(oldNode, contentType);
                        }
                        else
                        {
                            newNode = defaultDocTypeMapper.CreateNode(oldNode, contentType, parentRelation.NewKeyAsGuid);
                        }
                    }
                }
                else
                {
                    newNode = defaultDocTypeMapper.CreateRootNode(oldNode, contentType);
                }

                // Save
                try
                {
                    contentService.Save(newNode);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to save node {nodeName}. old node key: {oldNodeKey}", newNode.Name, oldNode.Key);
                    throw;
                }

                // Store new key lookup
                relationLookupService.StoreNewRelation(
                    newNode.Id.ToString(),
                    oldNode.Id,
                    newNode.Key,
                    oldNode.Key);

                logger.LogDebug("Created content node - {count}/{total} - \"{oldName}\" - {elapsedTime}", count, nodesToMigrate.Count, oldName, nodeStopwatch.Humanize());
            }

            if (failedDocTypes.Any())
            {
                logger.LogWarning("Failed to create nodes for the following DocTypes, as no mappers exist for them: \"{OldDocTypeAliases}\"", string.Join(',', failedDocTypes));
            }

            stopwatch.Stop();
            _hubService.SendMessage(1, $"Created Content Structure - {stopwatch.Humanize()}");
            logger.LogInformation("Created Content Structure - {elapsedTime}", stopwatch.Humanize());
        }

        private bool DoesNodeAlreadyExist(Guid oldKey)
        {
            // Do we have a key lookup entry?
            var relation = relationLookupService.GetRelationByOldKey(oldKey);
            var newKey = relation?.NewKeyAsGuid;
            if (newKey == null) { return false; }

            // Does a content node with the key exist?
            var contentNode = contentService.GetById((Guid)newKey);
            if (contentNode != null)
            {
                return true;
            }

            return false;
        }

        private IContent AddToTempParentNode(MigrationContent oldNode, string contentType)
        {
            _hubService.SendMessage(1, $"Saving '{oldNode.Key}' under temp node");
            logger.LogWarning("Saving '{oldKey}' under temp node", oldNode.Key);
            var tempParent = GetOrCreateTempParentContentNode();
            return defaultDocTypeMapper.CreateNode(oldNode, contentType, tempParent.Key);
        }

        private IContent GetOrCreateTempParentContentNode()
        {
            var tempParent = contentService.GetByLevel(1).FirstOrDefault(c => c.ContentType.Alias == "migrationTempParentNode");

            // Create the temp node if it doesn't exist
            if (tempParent == null)
            {
                // Create DocType if it doesn't exist
                var tempContentType = contentTypeService.Get("migrationTempParentNode");
                if (tempContentType == null)
                {
                    tempContentType = new ContentType(shortStringHelper, -1) { Alias = "migrationTempParentNode", Icon = "icon-truck color-deep-purple" };
                    tempContentType.Name = "Migration Temporary Parent Node";
                    contentTypeService.Save(tempContentType);
                }

                tempParent = contentService.CreateAndSave("Migration Temp Parent", -1, "migrationTempParentNode");
            }

            return tempParent;
        }
    }
}