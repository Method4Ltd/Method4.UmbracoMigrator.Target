using Method4.UmbracoMigrator.Target.Core.Mappers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Macros;
using MySite.Migration.Compositions;

namespace MySite.Migration.DocTypeMappers
{
    public class HomeMapper : IDocTypeMapping
    {
        private readonly MainCompositionMapper _mainCompositionMapper;
        private readonly GridMapper _gridMapper;

        private readonly IKeyTransformer _keyTransformer;

        public string DocTypeAlias => "home"; // The doctype I want to map to

        public HomeMapper(MainCompositionMapper mainCompositionMapper,
            GridCompositionMapper gridCompositionMapper,
            GridMapper gridMapper,
            IKeyTransformer keyTransformer)
        {
            _mainCompositionMapper = mainCompositionMapper;
            _gridMapper = gridMapper;
            _keyTransformer = keyTransformer;
        }

        public bool CanIMap(MigrationContent MigrationNode)
        {
            return MigrationNode.ContentType == "home"; // I can map old nodes of this doctype
        }

        public IContent MapNode(MigrationContent oldNode, IContent newNode, bool overwiteExisitingValues)
        {
            // Map Main Composition
            newNode = _mainCompositionMapper.MapNodeWithMainComposition(oldNode, newNode, overwiteExisitingValues);

            // Map Grid to Block Grid
            newNode = _gridMapper.MapNodeWithGridComposition(oldNode, newNode, overwiteExisitingValues, "Block Grid - Home");

            // Map home Properties
            var myPropertyAlias = HomeComponent.GetModelPropertyType(_publishedSnapshotAccessor, x => x.MyProperty)!.Alias;
            var myOtherPropertyAlias = HomeComponent.GetModelPropertyType(_publishedSnapshotAccessor, x => x.MyOtherProperty)!.Alias;
            var myRichTextPropertyAlias = HomeComponent.GetModelPropertyType(_publishedSnapshotAccessor, x => x.MyRichTextProperty)!.Alias;

            // Map myProperty
            if (newNode.HasProperty(myPropertyAlias) && (newNode.GetValue<int>(myPropertyAlias) == 0 || overwiteExisitingValues))
            {
                var oldValue = oldNode.Properties.First(x => x.Alias == "myProperty").GetDefaultValue;
                newNode.SetValue(myPropertyAlias, oldValue);
            }

            // Map MyOtherProperty
            if (newNode.HasProperty(myOtherPropertyAlias) && (newNode.GetValue<string>(myOtherPropertyAlias).IsNullOrWhiteSpace() || overwiteExisitingValues))
            {
                var oldValueEn = oldNode.Properties.First(x => x.Alias == "myOtherProperty").GetValue("en-GB");
                var oldValueCy = oldNode.Properties.First(x => x.Alias == "myOtherProperty").GetValue("cy-GB");
                newNode.SetValue(myOtherPropertyAlias, oldValueEn, "en-GB");
                newNode.SetValue(myOtherPropertyAlias, oldValueCy, "cy-GB");
            }

            // Map MyRichTextProperty
            if (newNode.HasProperty(myRichTextPropertyAlias) && (newNode.GetValue<int>(myRichTextPropertyAlias) == 0 || overwiteExisitingValues))
            {
                var oldValue = oldNode.Properties.First(x => x.Alias == "myRichTextProperty").GetDefaultValue;

                // Cleanse the RTE string just in case the macros have been rendered inline (this can happen for RTEs used in the old grid)
                // using Umbraco.Cms.Infrastructure.Macros for the MacroTagParser
                oldValue = MacroTagParser.FormatRichTextContentForPersistence(oldValue!);
                
                // Transform any Key references found in the Rich Text
                oldValue = _keyTransformer.TransformOldKeyReferences("Umbraco.TinyMCE", oldValue); // Can also use the constant from `Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TinyMce`

                // Save it
                newNode.SetValue(myPropertyAlias, oldValue);
            }

            return newNode;
        }
    }
}