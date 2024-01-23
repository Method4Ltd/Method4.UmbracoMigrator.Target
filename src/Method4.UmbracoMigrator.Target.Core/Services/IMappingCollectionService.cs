using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;

namespace Method4.UmbracoMigrator.Target.Core.Services
{
    public interface IMappingCollectionService
    {
        IDocTypeMapping? GetDocTypeMapping(MigrationContent migrationNode);
        IMediaTypeMapping? GetMediaTypeMapping(MigrationMedia migrationNode);
    }
}
