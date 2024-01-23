using Method4.UmbracoMigrator.Source.Core.Services;
using Method4.UmbracoMigrator.Target.Core.Factories;
using Method4.UmbracoMigrator.Target.Core.Hubs;
using Method4.UmbracoMigrator.Target.Core.MigrationSteps;
using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Method4.UmbracoMigrator.Target.Core.Models.Exceptions;
using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Controllers;
using static Umbraco.Cms.Core.Constants.Conventions;

namespace Method4.UmbracoMigrator.Target.Core.Controllers.Api
{
    public class MigratorTargetApiController : UmbracoAuthorizedApiController
    {
        private readonly IMigratorFileService _migratorFileService;
        private readonly IPreviewFactory _previewFactory;
        private readonly IMigrationPhase1 _migrationPhase1;
        private readonly IMigrationPhase2 _migrationPhase2;
        private readonly IMigrationPhase3 _migrationPhase3;
        private readonly IMigrationPhase4 _migrationPhase4;
        private readonly IRelationLookupService _relationLookupService;
        private readonly ILogger<MigratorTargetApiController> _logger;
        private readonly IContentService _contentService;
        private readonly IMediaService _mediaService;
        private readonly HubService _hubService;

        public MigratorTargetApiController(IMigratorFileService migratorFileService, 
            IPreviewFactory previewFactory, 
            IMigrationPhase1 migrationPhase1, 
            IMigrationPhase2 migrationPhase2, 
            IMigrationPhase3 migrationPhase3, 
            IMigrationPhase4 migrationPhase4, 
            IRelationLookupService relationLookupService, 
            ILogger<MigratorTargetApiController> logger, 
            IContentService contentService, 
            IMediaService mediaService, 
            IHubContext<MigrationHub> hubContext)
        {
            _migratorFileService = migratorFileService;
            _previewFactory = previewFactory;
            _migrationPhase1 = migrationPhase1;
            _migrationPhase2 = migrationPhase2;
            _migrationPhase3 = migrationPhase3;
            _migrationPhase4 = migrationPhase4;
            _relationLookupService = relationLookupService;
            _logger = logger;
            _contentService = contentService;
            _mediaService = mediaService;

            _hubService = new HubService(hubContext);
        }

        [HttpGet]
        public IActionResult GetMigrationSnapshots()
        {
            var snapshotFiles = _migratorFileService.GetAllMigrationSnapshotFiles();
            var snapshotPreviews = _previewFactory.ConvertToFilePreviews(snapshotFiles);
            return Ok(snapshotPreviews);
        }

        [HttpPost]
        public void BeginMigrationImport(ImportSettings settings)
        {
            _hubService.SendMessage(-1, $"Starting migration import of snapshot {settings.ChosenSnapshotName}");
            _logger.LogInformation("Starting migration import of snapshot {snapshotName}", settings.ChosenSnapshotName);
            _logger.LogInformation("Import settings: " +
                "[DontPublishAfterImport={dontPublishAfterImport}], " +
                "[OverwriteExistingValues={overwriteExistingValues}], " +
                "[DisableDefaultMappers={disableDefaultMappers}] " +
                "[CleanImport={cleanImport}]",
                settings.DontPublishAfterImport,
                settings.OverwriteExistingValues,
                settings.DisableDefaultMappers,
                settings.CleanImport);

            _migratorFileService.UnzipSnapshotFile(settings.ChosenSnapshotName!);
            var contentNodes = _migratorFileService.LoadContentXml();
            var mediaNodes = _migratorFileService.LoadMediaXml();

            if (settings.CleanImport)
            {
                _hubService.SendMessage(-1, "Cleaning site before import - Started");
                _logger.LogInformation("Cleaning site before import - Started");
                CleanBeforeImport();
                _relationLookupService.DeleteAllRelations();
                _hubService.SendMessage(-1, "Cleaning site before import  - Complete");
                _logger.LogInformation("Cleaning site before import  - Complete");
            }

            // Phase 1 - Node Structure
            try
            {
                _migrationPhase1.SetupMigrationPhase(contentNodes, mediaNodes, settings);
                _migrationPhase1.RunMigrationPhase();
            }
            catch (Exception ex)
            {
                _hubService.SendMessage(-1, "Migration failed on phase 1");
                _logger.LogError(ex, "Migration failed on phase 1");
                throw new MigrationFailedException(1, ex);
            }

            // Phase 2 - Media
            try
            {
                _migrationPhase2.SetupMigrationPhase(mediaNodes, settings);
                _migrationPhase2.RunMigrationPhase();
            }
            catch (Exception ex)
            {
                _hubService.SendMessage(-1, "Migration failed on phase 2");
                _logger.LogError(ex, "Migration failed on phase 2");
                throw new MigrationFailedException(2, ex);
            }

            // Phase 3 - Content
            try
            {
                _migrationPhase3.SetupMigrationPhase(contentNodes, settings);
                _migrationPhase3.RunMigrationPhase();
            }
            catch (Exception ex)
            {
                _hubService.SendMessage(-1, "Migration failed on phase 3");
                _logger.LogError(ex, "Migration failed on phase 3");
                throw new MigrationFailedException(3, ex);
            }

            // Phase 4 - Publishing
            try
            {
                _migrationPhase4.SetupMigrationPhase(contentNodes, mediaNodes, settings);
                _migrationPhase4.RunMigrationPhase();
            }
            catch (Exception ex)
            {
                _hubService.SendMessage(-1, "Migration failed on phase 4");
                _logger.LogError(ex, "Migration failed on phase 4");
                throw new MigrationFailedException(4, ex);
            }

            _hubService.SendMessage(-1, $"Completed migration import of snapshot {settings.ChosenSnapshotName}");
            _logger.LogInformation("Completed migration import of snapshot {snapshotName}", settings.ChosenSnapshotName);
        }

        [HttpGet]
        public IActionResult GetRelationsCount()
        {
            var count = _relationLookupService.CountRelations();
            return Ok(count);
        }

        [HttpPost]
        public IActionResult GetRelationByOldKey(string oldKey)
        {
            var relation = _relationLookupService.GetRelationByOldKey(oldKey);
            return Ok(relation);
        }

        [HttpPost]
        public IActionResult GetRelationByNewKey(string newKey)
        {
            var relation = _relationLookupService.GetRelationByNewKey(newKey);
            return Ok(relation);
        }

        [HttpPost]
        public IActionResult GetRelationByOldId(int oldId)
        {
            var relation = _relationLookupService.GetRelationByOldId(oldId.ToString());
            return Ok(relation);
        }

        [HttpPost]
        public IActionResult GetRelationByNewId(int newId)
        {
            var relation = _relationLookupService.GetRelationByNewId(newId.ToString());
            return Ok(relation);
        }

        [HttpDelete]
        public IActionResult DeleteAllRelations()
        {
            _relationLookupService.DeleteAllRelations();
            return Ok();
        }

        [HttpDelete]
        public IActionResult DeleteAllMigrationSnapshots()
        {
            _migratorFileService.DeleteAllMigrationSnapshotFiles();
            return Ok();
        }

        private void CleanBeforeImport()
        {
            var rootContent = _contentService.GetRootContent().ToList();
            var rootMedia = _mediaService.GetRootMedia().ToList();

            _hubService.SendMessage(0, "Moving all content to recycle bin");
            _logger.LogInformation("Moving all content to recycle bin");
            var rootContentTotal = rootContent.Count;
            var rootContentIndex = 0;
            foreach (var contentNode in rootContent)
            {
                rootContentIndex++;
                _hubService.SendMessage(0, $"Deleting root content {rootContentIndex}/{rootContentTotal}");
                var result = _contentService.MoveToRecycleBin(contentNode);
                if (result?.Success != true)
                {
                    _logger.LogError("Unable to move {nodeId} to recycle bin.", contentNode?.Id);
                    throw new Exception($"Unable to move {contentNode?.Id} to recycle bin.");
                }
            }

            _hubService.SendMessage(0, "Moving all media to recycle bin");
            _logger.LogInformation("Moving all media to recycle bin");
            var rootMediaTotal = rootMedia.Count;
            var rootMediaIndex = 0;
            foreach (var mediaNode in rootMedia)
            {
                rootMediaIndex++;
                _hubService.SendMessage(0, $"Deleting root content {rootMediaIndex}/{rootMediaTotal}");
                var result = _mediaService.MoveToRecycleBin(mediaNode);
                if (result.Success != true)
                {
                    _logger.LogError("Unable to move {nodeId} to recycle bin.", mediaNode?.Id);
                    throw new Exception($"Unable to move {mediaNode?.Id} to recycle bin.");
                }
            }

            _hubService.SendMessage(0, "Emptying content recycle bin");
            _logger.LogInformation("Emptying content recycle bin");
            var contentBinResult = _contentService.EmptyRecycleBin();
            if (contentBinResult?.Success != true)
            {
                _logger.LogError("Failed to empty the content recycle bin.");
                throw new Exception("Failed to empty the content recycle bin.");
            }

            _hubService.SendMessage(0, "Emptying media recycle bin");
            _logger.LogInformation("Emptying media recycle bin");
            var mediaBinResult = _mediaService.EmptyRecycleBin();
            if (mediaBinResult?.Success != true)
            {
                _logger.LogError("Failed to empty the media recycle bin.");
                throw new Exception("Failed to empty the media recycle bin.");
            }
        }
    }
}