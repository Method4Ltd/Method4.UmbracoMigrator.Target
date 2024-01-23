using Method4.UmbracoMigrator.Target.Core.Models.DataModels;

namespace Method4.UmbracoMigrator.Target.Core.Factories
{
    public interface IPreviewFactory
    {
        FilePreview ConvertToFilePreview(FileInfo item);
        List<FilePreview> ConvertToFilePreviews(IEnumerable<FileInfo> items);
    }

    public class PreviewFactory : IPreviewFactory
    {
        public PreviewFactory() { }

        public FilePreview ConvertToFilePreview(FileInfo item)
        {
            var filePreview = new FilePreview()
            {
                FileName = item.Name,
                CreateDate = item.CreationTime,
                SizeBytes = item.Length
            };
            return filePreview;
        }

        public List<FilePreview> ConvertToFilePreviews(IEnumerable<FileInfo> items)
        {
            var filePreviews = new List<FilePreview>();

            if (items?.Any() == true)
            {
                foreach (var item in items)
                {
                    filePreviews.Add(ConvertToFilePreview(item));
                }
            }
            return filePreviews;
        }
    }
}