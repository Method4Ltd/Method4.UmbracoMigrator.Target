using Method4.UmbracoMigrator.Target.Core.Mappers;
using Umbraco.Cms.Core.Composing;

namespace Method4.UmbracoMigrator.Target.Core.CollectionBuilders
{
    public class DocTypeMappingCollectionBuilder
        : OrderedCollectionBuilderBase<DocTypeMappingCollectionBuilder, DocTypeMappingCollection, IDocTypeMapping>
    {
        protected override DocTypeMappingCollectionBuilder This => this;
    }

    public class DocTypeMappingCollection : BuilderCollectionBase<IDocTypeMapping>
    {
        public DocTypeMappingCollection(Func<IEnumerable<IDocTypeMapping>> items) : base(items) { }
    }
}