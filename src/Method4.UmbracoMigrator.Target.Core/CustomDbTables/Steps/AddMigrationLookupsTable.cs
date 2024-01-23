using Method4.UmbracoMigrator.Target.Core.CustomDbTables.NPoco;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Method4.UmbracoMigrator.Target.Core.CustomDbTables.Steps
{
    internal class AddMigrationLookupsTable : MigrationBase
    {
        private readonly ILogger<AddMigrationLookupsTable> _logger;

        public AddMigrationLookupsTable(IMigrationContext context, ILogger<AddMigrationLookupsTable> logger) : base(context)
        {
            _logger = logger;
        }

        protected override void Migrate()
        {
            _logger.LogDebug("Beginning 'Method4 Umbraco Migrator Target' migration {migrationStep}", this.GetType().Name);

            if (TableExists("MigrationLookups") == false)
            {
                Create.Table<NodeRelationLookupPoco>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", "MigrationLookups");
            }
        }
    }
}