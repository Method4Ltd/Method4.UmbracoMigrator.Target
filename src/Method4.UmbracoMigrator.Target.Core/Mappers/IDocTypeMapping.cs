using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Umbraco.Cms.Core.Models;

namespace Method4.UmbracoMigrator.Target.Core.Mappers
{
    public interface IInternalDocTypeMapping : IBaseMapping<MigrationContent, IContent>
    {
        IContent CreateNode(MigrationContent oldNode, string contentTypeAlias, Guid parentKey);
        IContent CreateRootNode(MigrationContent oldNode, string contentTypeAlias);
    }

    public interface IDocTypeMapping : IBaseMapping<MigrationContent, IContent>
    {
        string DocTypeAlias { get; }
    }
}