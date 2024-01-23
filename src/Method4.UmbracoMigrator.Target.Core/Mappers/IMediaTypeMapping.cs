using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Umbraco.Cms.Core.Models;

namespace Method4.UmbracoMigrator.Target.Core.Mappers
{
    public interface IInternalMediaTypeMapping : IBaseMapping<MigrationMedia, IMedia>
    {
        IMedia CreateNode(MigrationMedia oldNode, string contentTypeAlias, Guid parentKey);
        IMedia CreateRootNode(MigrationMedia oldNode, string contentTypeAlias);
    }

    public interface IMediaTypeMapping : IBaseMapping<MigrationMedia, IMedia>
    {
        string MediaTypeAlias { get; }
    }
}