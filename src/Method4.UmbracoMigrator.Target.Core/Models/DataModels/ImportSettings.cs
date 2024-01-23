namespace Method4.UmbracoMigrator.Target.Core.Models.DataModels
{
    public class ImportSettings
    {
        public bool DontPublishAfterImport { get; set; }
        public bool OverwriteExistingValues { get; set; }
        public bool DisableDefaultMappers { get; set; }
        public bool CleanImport { get; set; }
        public string? ChosenSnapshotName { get; set; }
    }
}