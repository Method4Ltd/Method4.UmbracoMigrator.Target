using Method4.UmbracoMigrator.Target.Core.Helpers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using MySite.Models.UmbracoModels;

namespace MySite.Migration.CompositionMappers
{
    public class MainCompositionMapper
    {
        private readonly ILogger<MainCompositionMapper> _logger;
        private readonly IKeyTransformer _keyTransformer;
        private readonly IPropertyEditorConverter _propertyEditorConverter;
        private readonly IPublishedSnapshotAccessor _publishedSnapshotAccessor;

        public MainCompositionMapper(ILogger<MainCompositionMapper> logger, 
            IKeyTransformer keyTransformer,
            IPropertyEditorConverter propertyEditorConverter,
            IPublishedSnapshotAccessor publishedSnapshotAccessor)
        {
            _logger = logger;
            _keyTransformer = keyTransformer;
            _propertyEditorConverter = propertyEditorConverter;
            _publishedSnapshotAccessor = publishedSnapshotAccessor;
        }

        public IContent MapNodeWithMainComposition(MigrationContent oldNode, IContent newNode, bool overwiteExisitingValues)
        {
            var titleAlias = Main.GetModelPropertyType(_publishedSnapshotAccessor, x => x.Title)!.Alias;
            var pageDescriptionAlias = Main.GetModelPropertyType(_publishedSnapshotAccessor, x => x.PageDescription)!.Alias;
            var socialShareImageAlias = Main.GetModelPropertyType(_publishedSnapshotAccessor, x => x.SocialShareImage)!.Alias;

            // Map title
            if (newNode.HasProperty(titleAlias) && (newNode.GetValue<string>(titleAlias).IsNullOrWhiteSpace() || overwiteExisitingValues))
            {
                var oldValueEn = oldNode.Properties.First(x => x.Alias == "title").GetValue("en-GB");
                var oldValueCy = oldNode.Properties.First(x => x.Alias == "title").GetValue("cy-GB");
                newNode.SetValue(titleAlias, oldValueEn, "en-GB");
                newNode.SetValue(titleAlias, oldValueCy, "cy-GB");
            }

            // Map pageDescription
            if (newNode.HasProperty(pageDescriptionAlias) && (newNode.GetValue<string>(pageDescriptionAlias).IsNullOrWhiteSpace() || overwiteExisitingValues))
            {
                var oldValueEn = oldNode.Properties.First(x => x.Alias == "pageDescription").GetValue("en-GB");
                var oldValueCy = oldNode.Properties.First(x => x.Alias == "pageDescription").GetValue("cy-GB");
                newNode.SetValue(pageDescriptionAlias, oldValueEn, "en-GB");
                newNode.SetValue(pageDescriptionAlias, oldValueCy, "cy-GB");
            }

            // Map socialShareImage
            if (newNode.HasProperty(socialShareImageAlias) && (newNode.GetValue<string>(socialShareImageAlias).IsNullOrWhiteSpace() || overwiteExisitingValues))
            {
                var oldValue = oldNode.Properties.First(x => x.Alias == "socialShareImage").GetDefaultValue;
                string? newValue = null;
                if (oldValue.IsNullOrWhiteSpace() == false)
                {
                    // First convert the GUID references into our new GUIDs
                    oldValue = _keyTransformer.TransformOldKeyReferences("Umbraco.MediaPicker", oldValue);
                    // Convert the old MediaPicker value format into the new MediaPicker3 format
                    newValue = _propertyEditorConverter.ConvertMediaPickerValueToMediaPicker3Value(oldValue);
                }
                newNode.SetValue(socialShareImageAlias, newValue);
            }

            return newNode;
        }
    }
}
