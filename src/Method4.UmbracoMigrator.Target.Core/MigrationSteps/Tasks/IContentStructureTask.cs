using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps.Tasks;

public interface IContentStructureTask
{
    /// <summary>
    /// Imports and saves the migration nodes, into the content tree, but without any property data
    /// </summary>
    /// <param name="nodesToMigrate"></param>
    void CreateContentNodeStructure(List<MigrationContent> nodesToMigrate);
}