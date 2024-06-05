using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps.Tasks;

public interface IMediaStructureTask
{
    /// <summary>
    /// Imports and saves the migration nodes, into the media tree, but without any property data
    /// </summary>
    /// <param name="nodesToMigrate"></param>
    void CreateMediaNodeStructure(List<MigrationMedia> nodesToMigrate);
}