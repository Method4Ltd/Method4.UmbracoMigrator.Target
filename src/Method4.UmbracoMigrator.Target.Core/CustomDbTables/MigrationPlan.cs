using Method4.UmbracoMigrator.Target.Core.CustomDbTables.Steps;
using Umbraco.Cms.Core.Packaging;

namespace Method4.UmbracoMigrator.Target.Core.CustomDbTables
{
    public class MigrationPlan : PackageMigrationPlan
    {
        public MigrationPlan() : base("Method4UmbracoMigratorTarget") { }

        public override bool IgnoreCurrentState => false;

        protected override void DefinePlan()
        {
            From(string.Empty);
            To<AddMigrationLookupsTable>("method4UmbracoMigratorTarget-initial");
        }
    }
}