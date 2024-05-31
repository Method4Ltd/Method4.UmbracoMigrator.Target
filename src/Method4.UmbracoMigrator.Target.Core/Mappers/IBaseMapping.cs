using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Umbraco.Cms.Core.Models;

namespace Method4.UmbracoMigrator.Target.Core.Mappers
{
    public interface IBaseMapping<TObject1, TObject2> where TObject1 : MigrationBase where TObject2 : IContentBase
    {
        bool CanIMap(TObject1 migrationNode);
        TObject2 MapNode(TObject1 oldNode, TObject2 newNode, bool overwriteExistingValues);
    }
}