using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Macros;
using MySite.Migration.Compositions;

namespace MySite.Migration.DocTypeMappers
{
    public class ContentPageMapper : IDocTypeMapping
    {
        private readonly GridMapper _gridMapper;
        private readonly IKeyTransformer _keyTransformer;
        private readonly IRichTextWithMacrosMapper _richTextMapper;

        public string DocTypeAlias => "contentPage"; // The doctype I want to map to

        public ContentPageMapper(
            IRichTextWithMacrosMapper richTextMapper,
            IKeyTransformer keyTransformer)
        {
            _keyTransformer = keyTransformer;
            _richTextMapper = richTextMapper;
        }

        public bool CanIMap(MigrationContent MigrationNode)
        {
            return MigrationNode.ContentType == "contentPage"; // I can map old nodes of this doctype
        }

        public IContent MapNode(MigrationContent oldNode, IContent newNode, bool overwiteExisitingValues)
        {
            /// Map myProperty
            if (newNode.HasProperty(myPropertyAlias) && (newNode.GetValue<int>(myPropertyAlias) == 0 || overwiteExisitingValues))
            {
                var oldValue = oldNode.Properties.First(x => x.Alias == "myProperty").GetDefaultValue;
                newNode.SetValue(myPropertyAlias, oldValue);
            }

            /// Map MyOtherProperty
            if (newNode.HasProperty(myOtherPropertyAlias) && (newNode.GetValue<string>(myOtherPropertyAlias).IsNullOrWhiteSpace() || overwiteExisitingValues))
            {
                var oldValueEn = oldNode.Properties.First(x => x.Alias == "myOtherProperty").GetValue("en-GB");
                var oldValueCy = oldNode.Properties.First(x => x.Alias == "myOtherProperty").GetValue("cy-GB");
                newNode.SetValue(myOtherPropertyAlias, oldValueEn, "en-GB");
                newNode.SetValue(myOtherPropertyAlias, oldValueCy, "cy-GB");
            }

            /// Map MyRichTextProperty
            if (newNode.HasProperty(myRichTextPropertyAlias) && (newNode.GetValue<int>(myRichTextPropertyAlias) == 0 || overwiteExisitingValues))
            {
                var oldValue = oldNode.Properties.First(x => x.Alias == "myRichTextProperty").GetDefaultValue;

                /// Use keyTransformer to update any keys in the rich text to their new value
                var oldRichText = _keyTransformer.TransformOldKeyReferences("Umbraco.TinyMCE", oldValue);

                /// Use the rich text mapper to create the new value, it will come back as a
                /// serialized object, so we can just set the property with the value returned
                newNode.SetValue(myPropertyAlias, _richTextMapper.MapRichText(oldRichText));
            }

            return newNode;
        }
    }
}