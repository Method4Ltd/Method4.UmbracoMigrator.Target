using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Method4.UmbracoMigrator.Target.Core.CustomDbTables.NPoco
{
    [TableName("MigrationLookups")]
    [PrimaryKey("OldId", AutoIncrement = false)]
    [ExplicitColumns]
    internal class NodeRelationLookupPoco
    {
        [PrimaryKeyColumn(AutoIncrement = false)]
        [Column("OldId")]
        public string OldId { get; set; }

        [Column("NewId")]
        public string NewId { get; set; }

        [Column("OldKey")]
        public string OldKey { get; set; }

        [Column("NewKey")]
        public string NewKey { get; set; }
    }
}