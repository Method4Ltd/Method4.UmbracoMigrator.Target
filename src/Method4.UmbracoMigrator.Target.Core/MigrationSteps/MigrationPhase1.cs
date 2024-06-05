using Method4.UmbracoMigrator.Target.Core.Extensions;
using Method4.UmbracoMigrator.Target.Core.Hubs;
using Method4.UmbracoMigrator.Target.Core.MigrationSteps.Tasks;
using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Umbraco.Cms.Core.Services;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps
{
    internal class MigrationPhase1 : IMigrationPhase1
    {
        private readonly IContentService _contentService;
        private readonly IMediaService _mediaService;
        private readonly IRelationLookupService _relationLookupService;
        private readonly ILogger<MigrationPhase1> _logger;
        private readonly HubService _hubService;

        private readonly IContentStructureTask _contentStructureTask;
        private readonly IMediaStructureTask _mediaStructureTask;

        private List<MigrationContent>? _contentNodesToMigrate = null;
        private List<MigrationMedia>? _mediaNodesToMigrate = null;
        private ImportSettings? _settings = null;

        public MigrationPhase1(IContentService contentService, IMediaService mediaService,
            IRelationLookupService relationLookupService,
            ILogger<MigrationPhase1> logger,
            IHubContext<MigrationHub> hubContext, 
            IContentStructureTask contentStructureTask, IMediaStructureTask mediaStructureTask)
        {
            _contentService = contentService;
            _mediaService = mediaService;
            _relationLookupService = relationLookupService;
            _logger = logger;

            _contentStructureTask = contentStructureTask;
            _mediaStructureTask = mediaStructureTask;

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

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _contentStructureTask.CreateContentNodeStructure(_contentNodesToMigrate);
            _mediaStructureTask.CreateMediaNodeStructure(_mediaNodesToMigrate);

            MoveTempNodes();

            stopwatch.Stop();

            _hubService.SendMessage(1, $"Completed Migration Phase 1 - {stopwatch.Humanize()}");
            _logger.LogInformation("Completed Migration Phase 1 - {elapsedMilliseconds}", stopwatch.Humanize());
#pragma warning restore CS8604 // Possible null reference argument.
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
    }
}