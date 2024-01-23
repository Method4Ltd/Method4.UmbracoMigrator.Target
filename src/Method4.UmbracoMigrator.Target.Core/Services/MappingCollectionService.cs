using Method4.UmbracoMigrator.Target.Core.CollectionBuilders;
using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;

namespace Method4.UmbracoMigrator.Target.Core.Services
{
    public class MappingCollectionService : IMappingCollectionService
    {
        private readonly IEnumerable<IDocTypeMapping> _docTypeMappings;
        private readonly IEnumerable<IMediaTypeMapping> _mediaTypeMappings;

        public MappingCollectionService(DocTypeMappingCollection docTypeMappings, MediaTypeMappingCollection mediaTypeMappings)
        {
            _docTypeMappings = docTypeMappings;
            _mediaTypeMappings = mediaTypeMappings;
        }

        public IDocTypeMapping? GetDocTypeMapping(MigrationContent migrationNode)
        {
            return _docTypeMappings.FirstOrDefault(x => x.CanIMap(migrationNode));
        }

        public IMediaTypeMapping? GetMediaTypeMapping(MigrationMedia migrationNode)
        {
            return _mediaTypeMappings.FirstOrDefault(x => x.CanIMap(migrationNode));
        }
    }
}