using Method4.UmbracoMigrator.Target.Core.Extensions;
using Method4.UmbracoMigrator.Target.Core.Hubs;
using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps
{
    public class MigrationPhase4 : IMigrationPhase4
    {
        private readonly ILogger<MigrationPhase4> _logger;
        private readonly IRelationLookupService _relationLookupService;
        private readonly IContentService _contentService;
        private readonly IMediaService _mediaService;
        private readonly IPublicAccessService _publicAccessService;
        private readonly IContentTypeService _contentTypeService;
        private readonly HubService _hubService;

        private List<MigrationContent>? _contentNodesToMigrate;
        private List<MigrationMedia>? _mediaNodesToMigrate;
        private ImportSettings? _settings;

        public MigrationPhase4(ILogger<MigrationPhase4> logger,
            IRelationLookupService relationLookupService,
            IContentService contentService,
            IMediaService mediaService,
            IPublicAccessService publicAccessService,
            IContentTypeService contentTypeService,
            IHubContext<MigrationHub> hubContext)
        {
            _logger = logger;
            _relationLookupService = relationLookupService;
            _contentService = contentService;
            _mediaService = mediaService;
            _publicAccessService = publicAccessService;
            _contentTypeService = contentTypeService;

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
            _hubService.SendMessage(4, "Starting Migration Phase 4");
            _logger.LogInformation("Starting Migration Phase 4");

            var contentNodesToMigrateTotal = _contentNodesToMigrate!.Count;
            var contentNodesCount = 0;
            foreach (var migrationContent in _contentNodesToMigrate!)
            {
                contentNodesCount++;
                _hubService.SendMessage(4, $"Setting content publish status - {contentNodesCount}/{contentNodesToMigrateTotal} - \"{migrationContent.Name.Truncate()}\"");

                var relation = _relationLookupService.GetRelationByOldKey(migrationContent.Key);
                var newKey = relation?.NewKeyAsGuid;
                if (newKey == null) { continue; }

                var contentNode = _contentService.GetById((Guid)newKey);
                if (contentNode == null) { continue; }

                // Trash it?
                if (migrationContent.Trashed)
                {
                    TrashContent((Guid)newKey);
                    continue;
                }

                // Publish it?
                if (_settings.DontPublishAfterImport == false)
                {
                    var contentType = _contentTypeService.Get(contentNode.ContentType.Alias);
                    var newNodeVaries = contentType?.VariesByCulture() == true;
                    var oldNodeVaries = migrationContent.VariesByCulture;

                    foreach (var publishedStatus in migrationContent.PublishedStatus)
                    {
                        if (publishedStatus.Published == false) { continue; }

                        if (newNodeVaries)
                        {
                            switch (oldNodeVaries)
                            {
                                // Skip default if we have variants
                                case true when publishedStatus.Culture != "default":
                                case false when publishedStatus.Culture == "default":
                                    PublishContent((Guid)newKey, publishedStatus.Culture);
                                    break;
                            }
                        }
                        else
                        {
                            if (publishedStatus.Culture == "default")
                            {
                                PublishContent((Guid)newKey, publishedStatus.Culture);
                            }
                        }
                    }
                }

                // Schedule it?
                foreach (var scheduledStatus in migrationContent.ScheduledStatus)
                {
                    if (scheduledStatus != null) { continue; }

                    // TODO Schedule content
                    //ScheduleContent((Guid)newKey, scheduledStatus);
                }

                // Set Public Access on it?
                if (migrationContent.PublicAccess != null)
                {
                    SetPublicAccess((Guid)newKey, migrationContent.PublicAccess);
                }
            }

            var mediaNodesToMigrateTotal = _mediaNodesToMigrate!.Count;
            var mediaNodesCount = 0;
            foreach (var media in _mediaNodesToMigrate!)
            {
                mediaNodesCount++;
                _hubService.SendMessage(4, $"Setting media publish status - {mediaNodesCount}/{mediaNodesToMigrateTotal} - \"{media.Name.Truncate()}\"");

                var relation = _relationLookupService.GetRelationByOldKey(media.Key);
                var newKey = relation?.NewKeyAsGuid;
                if (newKey == null) { continue; }

                // Trash it?
                if (media.Trashed)
                {
                    TrashMedia((Guid)newKey);
                    continue;
                }
            }

            _hubService.SendMessage(4, "Completed Migration Phase 4");
            _logger.LogInformation("Completed Migration Phase 4");
        }

        private void PublishContent(Guid contentKey, string culture)
        {
            var contentNode = _contentService.GetById(contentKey);
            if (contentNode == null)
            {
                _logger.LogWarning("Could not publish the {culture} variant of content node {key}, as it could not be found.", culture, contentKey);
                return;
            }

            var result = _contentService.SaveAndPublish(contentNode, (culture == "default" ? "*" : culture));
            if (result?.Success == false)
            {
                _logger.LogError("Failed to publish the {culture} variant of content node {id}.", culture, contentNode.Id);
            }
        }

        private void ScheduleContent(Guid contentKey, MigrationScheduleStatus scheduleStatus)
        {
            throw new NotImplementedException();
        }

        private void TrashContent(Guid contentKey)
        {
            var contentNode = _contentService.GetById(contentKey);
            if (contentNode == null)
            {
                _logger.LogWarning("Could not trash content node {key}, as it could not be found.", contentKey);
                return;
            }

            var result = _contentService.MoveToRecycleBin(contentNode);
            if (result?.Success == false)
            {
                _logger.LogError("Failed to trash content node {id}.", contentNode.Id);
            }
        }

        private void TrashMedia(Guid contentKey)
        {
            var mediaNode = _mediaService.GetById(contentKey);
            if (mediaNode == null)
            {
                _logger.LogWarning("Could not trash media node {key}, as it could not be found.", contentKey);
                return;
            }

            var result = _mediaService.MoveToRecycleBin(mediaNode);
            if (result.Success == false)
            {
                _logger.LogError("Failed to trash media node {id}. {excemptionMessage}", mediaNode.Id, result.Exception?.Message);
            }
        }

        private void SetPublicAccess(Guid contentKey, MigrationPublicAccess migrationPublicAccess)
        {
            var contentNode = _contentService.GetById(contentKey);
            if (contentNode == null)
            {
                _logger.LogWarning("Could not set Public Access on content node {key}, as it could not be found.", contentKey);
                return;
            }

            // Check if the content already has a PA Entry
            var currentPaEntry = _publicAccessService.GetEntryForContent(contentNode);
            if (currentPaEntry != null)
            {
                if (_settings!.OverwriteExistingValues == false) { return; }
                _publicAccessService.Delete(currentPaEntry);
            }

            // Build our new PA Entry
            var paEntryId = Guid.NewGuid();
            var rules = new List<PublicAccessRule>();

            IContent? loginNode = null;
            IContent? noAccessNode = null;

            // Get the login node
            if (migrationPublicAccess.LoginNodeKey != null)
            {
                var relation = _relationLookupService.GetRelationByOldKey((Guid)migrationPublicAccess.LoginNodeKey);
                var loginNodeKey = relation?.NewKeyAsGuid;
                if (loginNodeKey != null)
                {
                    loginNode = _contentService.GetById((Guid)loginNodeKey);
                }
                else
                {
                    _logger.LogError("Could not find key lookup for {oldKey}", migrationPublicAccess.LoginNodeKey.ToString());
                }
            }

            if (loginNode == null)
            {
                _logger.LogError("Skipping Public Access Entry for node {nodeId} [{nodeKey}], as the selected Login node is null", contentNode.Id, contentKey);
                return;
            }

            // Get the no access node
            if (migrationPublicAccess.NoAccessNodeKey != null)
            {
                var relation = _relationLookupService.GetRelationByOldKey((Guid)migrationPublicAccess.NoAccessNodeKey);
                var noAccessKey = relation?.NewKeyAsGuid;
                if (noAccessKey != null)
                {
                    noAccessNode = _contentService.GetById((Guid)noAccessKey);
                }
                else
                {
                    _logger.LogError("Could not find key lookup for {oldKey}", migrationPublicAccess.NoAccessNodeKey.ToString());
                }
            }

            if (noAccessNode == null)
            {
                _logger.LogError("Skipping Public Access Entry for node {nodeId} [{nodeKey}], as the selected No Access node is null", contentNode.Id, contentKey);
                return;
            }

            // Build the rules
            foreach (var migrationPaRule in migrationPublicAccess.Rules)
            {
                var rule = new PublicAccessRule(Guid.NewGuid(), paEntryId)
                {
                    RuleType = migrationPaRule.Type,
                    RuleValue = migrationPaRule.Value
                };
                rules.Add(rule);
            }

            var paEntry = new PublicAccessEntry(
                contentNode,
                loginNode,
                noAccessNode,
                rules);

            try
            {
                _publicAccessService.Save(paEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Public Access entry for node {nodeId} [{nodeKey}]", contentNode.Id, contentKey);
            }
        }
    }
}
