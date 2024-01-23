namespace Method4.UmbracoMigrator.Target.Core.Models.MigrationModels
{
    public class MigrationBase
    {
        // Node identity
        public Guid Key { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }

        // Node Info
        public Guid ParentKey { get; set; }
        public bool Trashed { get; set; }
        public string ContentType { get; set; }
        public DateTime CreateDate { get; set; }
        public List<MigrationNodeName> NodeNames { get; set; }
        public int SortOrder { get; set; }

        public MigrationBase(Guid key,
            string id,
            string name,
            int level,
            Guid parentKey,
            bool trashed,
            string contentType,
            DateTime createDate,
            List<MigrationNodeName> nodeNames,
            int sortOrder)
        {
            Key = key;
            Id = id;
            Name = name;
            Level = level;
            ParentKey = parentKey;
            Trashed = trashed;
            ContentType = contentType;
            CreateDate = createDate;
            NodeNames = nodeNames;
            SortOrder = sortOrder;
        }
    }

    public class MigrationNodeName
    {
        public string Culture { get; set; }
        public string Name { get; set; }

        public MigrationNodeName(string culture, string name)
        {
            Culture = culture;
            Name = name;
        }
    }
}