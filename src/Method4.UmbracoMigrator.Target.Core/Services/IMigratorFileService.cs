using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;

namespace Method4.UmbracoMigrator.Source.Core.Services
{
    public interface IMigratorFileService
    {
        List<FileInfo> GetAllMigrationSnapshotFiles();
        void DeleteAllMigrationSnapshotFiles();
        void DeleteMigrationSnapshotFile(string fileName);
        void DeleteTempFolder();
        void UnzipSnapshotFile(string fileName);
        List<MigrationContent> LoadContentXml();
        List<MigrationMedia> LoadMediaXml();
        void MigrateMediaFilesToBlob();
        void MigrateMediaFilesToDisk();
    }
}