namespace Method4.UmbracoMigrator.Target.Core.Models.MigrationModels
{
    public class MigrationMedia : MigrationBase
    {
        public List<MigrationProperty> Properties { get; set; }

        public MigrationMedia(Guid key,
            string id,
            string name,
            int level,
            Guid parentKey,
            bool trashed,
            string contentType,
            DateTime createDate,
            List<MigrationNodeName> nodeNames,
            int sortOrder,
            List<MigrationProperty> properties)
            : base(key, id, name, level, parentKey, trashed, contentType, createDate, nodeNames, sortOrder)
        {
            Properties = properties;
        }
    }
}
