namespace Method4.UmbracoMigrator.Target.Core.Models.MigrationModels
{
    public class CanMap
    {
        public bool CanBeMapped { get; set; }
        public string ContentTypeAlias { get; set; }

        public CanMap(bool canBeMapped, string contentTypeAlias)
        {
            CanBeMapped = canBeMapped;
            ContentTypeAlias = contentTypeAlias;
        }
    }
}
