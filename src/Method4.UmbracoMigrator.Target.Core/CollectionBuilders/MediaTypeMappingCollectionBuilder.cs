using Method4.UmbracoMigrator.Target.Core.Mappers;
using Umbraco.Cms.Core.Composing;

namespace Method4.UmbracoMigrator.Target.Core.CollectionBuilders
{
    public class MediaTypeMappingCollectionBuilder
        : OrderedCollectionBuilderBase<MediaTypeMappingCollectionBuilder, MediaTypeMappingCollection, IMediaTypeMapping>
    {
        protected override MediaTypeMappingCollectionBuilder This => this;
    }

    public class MediaTypeMappingCollection : BuilderCollectionBase<IMediaTypeMapping>
    {
        public MediaTypeMappingCollection(Func<IEnumerable<IMediaTypeMapping>> items) : base(items) { }
    }
}