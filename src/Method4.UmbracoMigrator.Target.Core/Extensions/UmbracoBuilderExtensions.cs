using Method4.UmbracoMigrator.Target.Core.CollectionBuilders;
using Umbraco.Cms.Core.DependencyInjection;

namespace Method4.UmbracoMigrator.Target.Core.Extensions
{
    internal static class UmbracoBuilderExtensions
    {
        public static DocTypeMappingCollectionBuilder DocTypeMappers(this IUmbracoBuilder builder) =>
            builder.WithCollectionBuilder<DocTypeMappingCollectionBuilder>();
        
        public static MediaTypeMappingCollectionBuilder MediaTypeMappers(this IUmbracoBuilder builder) =>
            builder.WithCollectionBuilder<MediaTypeMappingCollectionBuilder>();
    }
}
