using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Umbraco.Cms.Core.Models;

/// We unfortunatly need these as Umbraco will fail to boot if there are 0
/// implementations of the IDocTypeMapping/IMediaTypeMapping interfaces available
/// for the DI container.
namespace Method4.UmbracoMigrator.Target.Core.Mappers
{
    internal class DummyDocTypeMapper : IDocTypeMapping
    {
        public string DocTypeAlias => "DummyTypeMapping";

        public bool CanIMap(MigrationContent MigrationNode)
        {
            return false;
        }

        public IContent MapNode(MigrationContent oldNode, IContent newNode, bool overwiteExisitingValues)
        {
            throw new NotImplementedException();
        }
    }

    internal class DummyMediaTypeMapper : IMediaTypeMapping
    {
        public string MediaTypeAlias => "DummyTypeMapping";

        public bool CanIMap(MigrationMedia MigrationNode)
        {
            return false;
        }

        public IMedia MapNode(MigrationMedia oldNode, IMedia newNode, bool overwiteExisitingValues)
        {
            throw new NotImplementedException();
        }
    }
}