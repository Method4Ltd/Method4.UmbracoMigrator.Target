using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;

namespace Method4.UmbracoMigrator.Target.Core.MigrationSteps
{
    public interface IBaseMigrationPhase
    {
        void RunMigrationPhase();
    }

    public interface IMigrationPhase1 : IBaseMigrationPhase
    {
        void SetupMigrationPhase(List<MigrationContent> contentNodesToMigrate, List<MigrationMedia> mediaNodesToMigrate, ImportSettings settings);
    }

    public interface IMigrationPhase2 : IBaseMigrationPhase
    {
        void SetupMigrationPhase(List<MigrationMedia> mediaNodesToMigrate, ImportSettings settings);

    }

    public interface IMigrationPhase3 : IBaseMigrationPhase
    {
        void SetupMigrationPhase(List<MigrationContent> contentNodesToMigrate, ImportSettings settings);
    }

    public interface IMigrationPhase4 : IBaseMigrationPhase
    {
        void SetupMigrationPhase(List<MigrationContent> contentNodesToMigrate, List<MigrationMedia> mediaNodesToMigrate, ImportSettings settings);
    }
}