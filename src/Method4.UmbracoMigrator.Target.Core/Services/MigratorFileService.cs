using Azure.Storage.Blobs;
using Method4.UmbracoMigrator.Target.Core.Factories;
using Method4.UmbracoMigrator.Target.Core.Hubs;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Smidge.Models;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Method4.UmbracoMigrator.Target.Core.Extensions;

namespace Method4.UmbracoMigrator.Source.Core.Services
{
    public class MigratorFileService : IMigratorFileService
    {
        private readonly ILogger<MigratorFileService> _logger;
        private readonly IMigrationContentFactory _migrationContentFactory;
        private readonly IMigrationMediaFactory _migrationMediaFactory;
        private readonly IMigratorBlobService _migratorBlobService;
        private readonly HubService _hubService;

        private readonly string migrationSnapshotsPath;
        private readonly string migrationSnapshotTempPath;
        private readonly string tempContentXmlPath;
        private readonly string tempMediaXmlPath;
        private readonly string tempMediaFolderPath;
        private readonly string umbracoMediaDiskFolderPath;

        public MigratorFileService(ILogger<MigratorFileService> logger, 
            IWebHostEnvironment webHostEnvironment, 
            IMigrationContentFactory migrationContentFactory, 
            IMigrationMediaFactory migrationMediaFactory, 
            IMigratorBlobService migratorBlobService,
            IHubContext<MigrationHub> hubContext)
        {
            _logger = logger;
            _migrationContentFactory = migrationContentFactory;
            _migrationMediaFactory = migrationMediaFactory;
            _migratorBlobService = migratorBlobService;

            var webRootPath = webHostEnvironment.WebRootPath;
            migrationSnapshotsPath = Path.Combine(webRootPath, "migration-snapshots");
            migrationSnapshotTempPath = Path.Combine(migrationSnapshotsPath, "TEMP");
            tempContentXmlPath = Path.Combine(migrationSnapshotTempPath, "Content.xml");
            tempMediaXmlPath = Path.Combine(migrationSnapshotTempPath, "Media.xml");
            tempMediaFolderPath = Path.Combine(migrationSnapshotTempPath, "Media");
            umbracoMediaDiskFolderPath = Path.Combine(webRootPath, "media");

            if (!Directory.Exists(migrationSnapshotsPath)) { Directory.CreateDirectory(migrationSnapshotsPath); }

            _hubService = new HubService(hubContext);
        }

        /// <summary>
        /// Get all of the uploaded migration snapshot .zip files
        /// </summary>
        /// <returns></returns>
        public List<FileInfo> GetAllMigrationSnapshotFiles()
        {
            var snapshots = new List<FileInfo>();
            if (!Directory.Exists(migrationSnapshotsPath)) { Directory.CreateDirectory(migrationSnapshotsPath); }
            var snapshotPaths = Directory.GetFiles(migrationSnapshotsPath);

            foreach (var snapshotPath in snapshotPaths)
            {
                snapshots.Add(new FileInfo(snapshotPath));
            }

            return snapshots;
        }

        /// <summary>
        /// Delete all of the migration snapshot .zip files
        /// </summary>
        public void DeleteAllMigrationSnapshotFiles()
        {
            _logger.LogInformation("Deleting all migration snapshots");
            if (!Directory.Exists(migrationSnapshotsPath)) { Directory.CreateDirectory(migrationSnapshotsPath); }
            var snapshotPaths = Directory.GetFiles(migrationSnapshotsPath);
            foreach (var path in snapshotPaths)
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        /// <summary>
        /// Delete the migration snapshot .zip
        /// </summary>
        /// <param name="fileName"></param>
        public void DeleteMigrationSnapshotFile(string fileName)
        {
            _logger.LogInformation("Deleting migration snapshot {filename}", fileName);
            var snapshotPaths = Directory.GetFiles(migrationSnapshotsPath);
            foreach (var path in snapshotPaths)
            {
                if (path.Contains(fileName) == false) continue;

                if (File.Exists(path)) File.Delete(path);
            }
        }

        /// <summary>
        /// Unzip the contents of the migration snapshot .zip into the temp snapshot folder
        /// </summary>
        /// <param name="fileName"></param>
        /// <exception cref="Exception"></exception>
        public void UnzipSnapshotFile(string fileName)
        {
            _logger.LogInformation($"Unzipping snapshot file {fileName}, to temp folder");
            var zipPath = Path.Combine(migrationSnapshotsPath, fileName);
            if (File.Exists(zipPath) == false)
            {
                _logger.LogError("Migration snapshot '{fileName}' not found", fileName);
                throw new Exception($"Migration snapshot '{fileName}' not found!");
            }

            DeleteTempFolder(); // make sure it's empty

            Directory.CreateDirectory(migrationSnapshotTempPath);
            ZipFile.ExtractToDirectory(zipPath, migrationSnapshotTempPath);
        }

        /// <summary>
        /// Load the migration content from the content xml file that's in the temp snapshot folder
        /// </summary>
        /// <returns></returns>
        public List<MigrationContent> LoadContentXml()
        {
            _logger.LogInformation("Loading content XML");
            var contentXml = XElement.Load(tempContentXmlPath);
            var nodes = contentXml.Elements("Content");
            return _migrationContentFactory.ConvertFromXml(nodes);
        }

        /// <summary>
        /// Load the migration media from the media xml file that's in the temp snapshot folder
        /// </summary>
        /// <returns></returns>
        public List<MigrationMedia> LoadMediaXml()
        {
            _logger.LogInformation("Loading media XML");
            var contentXml = XElement.Load(tempMediaXmlPath);
            var nodes = contentXml.Elements("Media");
            return _migrationMediaFactory.ConvertFromXml(nodes);
        }

        /// <summary>
        /// Delete the temp snapshot folder
        /// </summary>
        public void DeleteTempFolder()
        {
            if (Directory.Exists(migrationSnapshotTempPath))
            {
                Directory.Delete(migrationSnapshotTempPath, true);
            }
        }

        /// <summary>
        /// Upload media files from the temp snapshot folder to blob storage
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void MigrateMediaFilesToBlob()
        {
            try
            {
                _logger.LogInformation("Starting upload of Media files to blob storage");
                if (!Directory.Exists(tempMediaFolderPath))
                {
                    _logger.LogWarning("No Media folder found in snapshot temp folder, skipping upload of media files to blob storage.");
                    return;
                }

                var blobContainerClient = _migratorBlobService.GetBlobContainerClient();
                var blobs = blobContainerClient.GetBlobs();

                var fileFolders = Directory.GetDirectories(tempMediaFolderPath);
                var fileFoldersTotal = fileFolders.Length;
                var fileFoldersCount = 0;

                // Umbraco media is stored with the folder structure of
                // '{randomId}/{fileName}', e.g. 'bgapfo4f/mycoolimage.png'
                foreach (var folderPath in fileFolders)
                {
                    fileFoldersCount++;
                    //var folderName = Path.GetDirectoryName(folderPath);
                    var folderName = (new DirectoryInfo(folderPath).Name);
                    var files = Directory.GetFiles(folderPath);
                    var filesTotal = files.Length;
                    var filesCount = 0;

                    foreach (var filePath in files)
                    {
                        filesCount++;
                        var fileName = Path.GetFileName(filePath);
                        var blobPath = Path.Combine("media", Path.Combine(folderName, fileName));

                        var blobClient = blobContainerClient.GetBlobClient(blobPath);
                        var fileStream = File.OpenRead(filePath);
                        blobClient.Upload(fileStream, true);
                        fileStream.Close();

                        _hubService.SendMessage(2, $"Uploaded - {fileFoldersCount}/{fileFoldersTotal} folders - {filesCount}/{filesTotal} files - \"{blobPath.Truncate()}\"");
                    }
                }

                _logger.LogInformation("Finished uploading Media to blob storage");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to upload media files to blob. {errorMessage}", ex.Message);
                throw new Exception("Failed to upload media files to blob.", ex);
            }
        }

        /// <summary>
        /// Upload media files from the temp snapshot folder to the umbraco media folder
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void MigrateMediaFilesToDisk()
        {
            try
            {
                _logger.LogInformation("Starting copy of Media to disk");
                if (!Directory.Exists(tempMediaFolderPath))
                {
                    _logger.LogWarning("No Media folder found in snapshot temp folder. skipping copy of media files to disk.");
                    return;
                }
                CopyDirectory(tempMediaFolderPath, umbracoMediaDiskFolderPath);
                _logger.LogInformation("Finished copying Media to disk");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed copying media files to disk folder. {errorMessage}", ex.Message);
                throw new Exception("Failed copying media files to disk folder", ex);
            }
        }

        private void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            if (Directory.Exists(targetDirectory)) { Directory.Delete(targetDirectory, true); }

            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        private void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);
            var files = source.GetFiles();
            var filesTotal = files.Length;
            var count = 0;

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                count++;
                if (fi.Name == "Web.config")
                {
                    _logger.LogInformation("Skipping {0}\\{1}", target.FullName, fi.Name);
                    continue;
                }

                _logger.LogTrace("Copying {0}\\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                _hubService.SendMessage(2, $"Copied - {count}/{filesTotal} - \"{fi.Name.Truncate()}\"");
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}