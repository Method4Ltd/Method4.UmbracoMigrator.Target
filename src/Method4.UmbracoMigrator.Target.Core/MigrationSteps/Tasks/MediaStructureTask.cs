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
    internal class MediaStructureTask(IMappingCollectionService migrationMapperService,
        IInternalMediaTypeMapping defaultMediaTypeMapper,
        IMediaTypeService mediaTypeService,
        IMediaService mediaService,
        IRelationLookupService relationLookupService,
        IShortStringHelper shortStringHelper,
        ILogger<MediaStructureTask> logger,
        IHubContext<MigrationHub> hubContext) : IMediaStructureTask
    {
        private readonly HubService _hubService = new(hubContext);

        /// <inheritdoc />
        public void CreateMediaNodeStructure(List<MigrationMedia> nodesToMigrate)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            logger.LogInformation("Creating Media Structure");

            var failedMediaTypes = new List<string>();
            nodesToMigrate = nodesToMigrate.OrderBy(x => x.Level).ToList();
            var count = 0;

            foreach (var oldNode in nodesToMigrate)
            {
                count++;
                var nodeStopwatch = new Stopwatch();
                nodeStopwatch.Start();

                var oldName = oldNode.Name.Truncate(20);

                _hubService.SendMessage(1, $"Creating media node - {count}/{nodesToMigrate.Count} - \"{oldName}\"");
                logger.LogDebug("Creating media node - {count}/{total} - \"{oldName}\"", count, nodesToMigrate.Count, oldName);

                var customMapping = migrationMapperService.GetMediaTypeMapping(oldNode);

                // If we can't map this node, then we will not create it
                if (customMapping == null && defaultMediaTypeMapper.CanIMap(oldNode) == false)
                {
                    _hubService.SendMessage(1, $"Could not create - \"{oldNode.Name.Truncate(20)}\" - No media type mapper exists for \"{oldNode.ContentType}\"");
                    logger.LogWarning("No mapper exists for the \"{OldMediaTypeAlias}\" MediaType", oldNode.ContentType);
                    if (failedMediaTypes.Contains(oldNode.ContentType) == false)
                    {
                        failedMediaTypes.Add(oldNode.ContentType);
                    }
                    continue;
                }

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
                        newNode = defaultMediaTypeMapper.CreateRootNode(oldNode, contentType);
                    }
                    else
                    {
                        var parentRelation = relationLookupService.GetRelationByOldKey(oldNode.ParentKey);
                        if (parentRelation == null)
                        {

                            // Add as a child to the temp node, if we haven't yet migrated it's parent
                            logger.LogWarning("Lookup for key '{parentKey}' Not found, '{oldKey}' will be added to the temp media node.", oldNode.ParentKey, oldNode.Key);
                            newNode = AddToTempParentNode(oldNode, contentType);
                        }
                        else
                        {
                            newNode = defaultMediaTypeMapper.CreateNode(oldNode, contentType, parentRelation.NewKeyAsGuid);
                        }
                    }
                }
                else
                {
                    newNode = defaultMediaTypeMapper.CreateRootNode(oldNode, contentType);
                }

                // Save
                try
                {
                    mediaService.Save(newNode);
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

                logger.LogDebug("Created media node - {count}/{total} - \"{oldName}\" - {elapsedTime}", count, nodesToMigrate.Count, oldName, nodeStopwatch.Humanize());
            }

            if (failedMediaTypes.Any())
            {
                logger.LogWarning("Failed to create nodes for the following MediaTypes, as no mappers exist for them: \"{OldDocTypeAliases}\"", string.Join(',', failedMediaTypes));
            }

            stopwatch.Stop();
            _hubService.SendMessage(1, $"Created Media Structure - {stopwatch.Humanize()}");
            logger.LogInformation("Created Media Structure - {elapsedTime}", stopwatch.Humanize());
        }

        private bool DoesNodeAlreadyExist(Guid oldKey)
        {
            // Do we have a key lookup entry?
            var relation = relationLookupService.GetRelationByOldKey(oldKey);
            var newKey = relation?.NewKeyAsGuid;
            if (newKey == null) { return false; }

            // Does a media node with the key exist?
            var mediaNode = mediaService.GetById((Guid)newKey);
            return mediaNode != null;
        }

        private IMedia AddToTempParentNode(MigrationMedia oldNode, string contentType)
        {
            _hubService.SendMessage(1, $"Saving '{oldNode.Key}' under temp node");
            logger.LogWarning("Saving '{oldKey}' under temp node", oldNode.Key);
            var tempParent = GetOrCreateTempParentMediaNode();
            return defaultMediaTypeMapper.CreateNode(oldNode, contentType, tempParent.Key);
        }

        private IMedia GetOrCreateTempParentMediaNode()
        {
            var tempParent = mediaService.GetByLevel(1)?.FirstOrDefault(c => c.ContentType.Alias == "migrationTempParentNode");

            // Create the temp node if it doesn't exist
            if (tempParent == null)
            {
                // Create DocType if it doesn't exist
                var tempContentType = mediaTypeService.Get("migrationTempParentNode");
                if (tempContentType == null)
                {
                    tempContentType = new MediaType(shortStringHelper, -1) { Alias = "migrationTempParentNode", Icon = "icon-truck color-deep-purple" };
                    tempContentType.Name = "Migration Temporary Parent Node";
                    mediaTypeService.Save(tempContentType);
                }

                tempParent = mediaService.CreateMediaWithIdentity("Migration Temp Parent", -1, "migrationTempParentNode");
            }

            return tempParent;
        }
    }
}
