namespace Method4.UmbracoMigrator.Target.Core.Models.DataModels
{
    public class ImportSettings
    {
        public bool DontPublishAfterImport { get; set; }
        public bool OverwriteExistingValues { get; set; }
        public bool DisableDefaultMappers { get; set; }
        public bool CleanImport { get; set; }
        public string? ChosenSnapshotName { get; set; }
        public bool PhaseOneEnabled { get; set; }
        public bool PhaseTwoEnabled { get; set; }
        public bool PhaseThreeEnabled { get; set; }
        public bool PhaseFourEnabled { get; set; }
    }
}