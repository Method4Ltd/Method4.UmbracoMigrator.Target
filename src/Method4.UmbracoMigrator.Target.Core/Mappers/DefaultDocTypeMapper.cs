using Method4.UmbracoMigrator.Target.Core.Helpers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Macros;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.Mappers
{
    public class DefaultDocTypeMapper : IInternalDocTypeMapping
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IContentService _contentService;
        private readonly IKeyTransformer _keyTransformer;
        private readonly IPropertyEditorConverter _propertyEditorConverter;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<DefaultDocTypeMapper> _logger;

        //private bool hasCultures;

        public DefaultDocTypeMapper(IContentTypeService contentTypeService,
            IContentService contentService,
            IKeyTransformer keyTransformer,
            IPropertyEditorConverter propertyEditorConverter,
            ILocalizationService localizationService,
            ILogger<DefaultDocTypeMapper> logger)
        {
            _contentTypeService = contentTypeService;
            _contentService = contentService;
            _keyTransformer = keyTransformer;
            _propertyEditorConverter = propertyEditorConverter;
            _localizationService = localizationService;
            _logger = logger;
        }

        public bool CanIMap(MigrationContent migrationNode)
        {
            // Look for a DocType that has the same alias/name as the old one
            var contentTypeAliases = _contentTypeService.GetAllContentTypeAliases();
            var contentTypeMatch = contentTypeAliases.Contains(migrationNode.ContentType);
            return contentTypeMatch;
        }

        public IContent CreateNode(MigrationContent oldNode, string contentTypeAlias, Guid parentKey)
        {
            return CreateNewNode(oldNode, contentTypeAlias, parentKey);
        }

        public IContent CreateRootNode(MigrationContent oldNode, string contentTypeAlias)
        {
            return CreateNewNode(oldNode, contentTypeAlias, null);
        }

        public IContent MapNode(MigrationContent oldNode, IContent newNode, bool overwriteExistingValues)
        {
            var contentType = _contentTypeService.Get(newNode.ContentType.Alias);

            // Map the default umbraco properties
            newNode.CreateDate = oldNode.CreateDate;
            newNode.UpdateDate = DateTime.Now;

            // Map properties that have the same aliases
            foreach (var oldProperty in oldNode.Properties)
            {
                if (newNode.HasProperty(oldProperty.Alias) == false) { continue; }

                var newProperty = newNode.Properties.First(x => x.Alias == oldProperty.Alias);
                var hasValue = (newProperty.GetValue()?.ToString() ?? "").IsNullOrWhiteSpace() == false;

                if (hasValue && overwriteExistingValues == false) { continue; }

                var oldPropertyVaries = oldProperty.Values.Count > 1 || oldProperty.Values[0]?.Culture != "default";
                var newPropertyVaries = newProperty.PropertyType.Variations == ContentVariation.Culture;
                var oldNodeDefaultCulture = oldNode.GetDefaultCulture();

                foreach (var oldValue in oldProperty.Values)
                {
                    // Skip old variant values if the new property doesn't vary by culture
                    if (oldPropertyVaries && !newPropertyVaries && (oldValue.Culture != oldNodeDefaultCulture)) { continue; }

                    var value = oldValue.Value;

                    // Save a NULL value if the old value is NULL or WhiteSpace
                    if (value.IsNullOrWhiteSpace())
                    {
                        try
                        {
                            if (newProperty.PropertyType.Variations == ContentVariation.Nothing)
                            {
                                newNode.SetValue(oldProperty.Alias, null);
                            }
                            else
                            {
                                // TODO support variations properly
                            }
                            continue;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to save NULL value for property {propertyAlias} on node {nodeId} [{nodeKey}]", oldProperty.Alias, newNode.Id, newNode.Key);
                            throw new Exception($"Failed to save NULL value for property {oldProperty.Alias} on node {newNode.Id} [{newNode.Key}]", ex);
                        }
                    }

                    // Update Key references, if we can
                    if (_keyTransformer.CanHaveKeyTransformed(oldProperty.PropertyEditorAlias))
                    {
                        try
                        {
                            value = _keyTransformer.TransformOldKeyReferences(oldProperty.PropertyEditorAlias, value!);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to Transform Old Key References in property {propertyAlias} on node {nodeId} [{nodeKey}]. Value: {value}", oldProperty.Alias, newNode.Id, newNode.Key, value);
                            throw new Exception($"Failed to Transform Old Key References in property {oldProperty.Alias} on node {newNode.Id} [{newNode.Key}]. Value: {value}", ex);
                        }
                    }

                    // Convert to new format, if we can
                    if (_propertyEditorConverter.CanConvert(newProperty, oldProperty))
                    {
                        try
                        {
                            value = _propertyEditorConverter.Convert(newProperty.PropertyType.PropertyEditorAlias, oldProperty.PropertyEditorAlias, value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to convert property {propertyAlias}, on node {nodeId} [{nodeKey}], from {oldPropertyEditorAlias} to {newPropertyEditorAlias} format. Value: {value}",
                                oldProperty.Alias, newNode.Id, newNode.Key, oldProperty.PropertyEditorAlias, newProperty.PropertyType.PropertyEditorAlias, value);
                            throw new Exception($"Failed to convert property {oldProperty.Alias}, on node {newNode.Id} [{newNode.Key}], from {oldProperty.PropertyEditorAlias} to {newProperty.PropertyType.PropertyEditorAlias} format. Value: {value}", ex);
                        }
                    }

                    // If it's an RTE, we need to cleanse it, just in case the macros have been rendered inline.
                    if (oldProperty.PropertyEditorAlias == Constants.PropertyEditors.Aliases.TinyMce)
                    {
                        // NOTE: There's an issue in Umbraco where RTEs in the old grid can, under certain circumstances, have their Macros rendered inline in the db...
                        //
                        // The Macro in the RTE value should be like this:
                        //  <?UMBRACO_MACRO macroAlias="MyMacro" dataMyProperty="This is a Macro" />
                        //
                        // But, can instead be rendered inline like this:
                        //<div class="umb-macro-holder MyMacro umb-macro-mce_3 mceNonEditable">
                        //      <!-- <?UMBRACO_MACRO macroAlias="MyMacro" dataMyProperty="This is a Macro" /> -->
                        //      <ins>
                        //           <div>
                        //                ...
                        //           </div>
                        //      </ins>
                        // </div>
                        //
                        // Passing it through Umbraco's FormatRichTextContentForPersistence() method should fix this.
                        try
                        {
                            // Try catch as this will throw if the Macro comment is at the root of the document
                            value = MacroTagParser.FormatRichTextContentForPersistence(value!);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to Format Rich Text Content For Persistence on property {propertyAlias}, for node {nodeId} [{nodeKey}]. Macros may not render correctly.",
                                oldProperty.Alias, newNode.Id, newNode.Key);
                            //throw;
                        }
                    }

                    // Attempt to save the transformed value
                    try
                    {
                        if (oldPropertyVaries && newPropertyVaries)
                        {
                            if (oldValue.Culture == "default")
                            {
                                newNode.SetValue(oldProperty.Alias, value, oldNodeDefaultCulture);
                            }
                            else
                            {
                                newNode.SetValue(oldProperty.Alias, value, oldValue.Culture);
                            }
                        }
                        else if (!oldPropertyVaries && newPropertyVaries)
                        {
                            newNode.SetValue(oldProperty.Alias, value, oldNodeDefaultCulture);
                        }
                        else
                        {
                            newNode.SetValue(oldProperty.Alias, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save property {propertyAlias} on node {nodeId} [{nodeKey}]. Value: {value}", oldProperty.Alias, newNode.Id, newNode.Key, value);
                        throw new Exception($"Failed to save property {oldProperty.Alias} on node {newNode.Id} [{newNode.Key}]. Value: {value}", ex);
                    }
                }
            }

            return newNode;
        }

        private IContent CreateNewNode(MigrationContent oldNode, string contentTypeAlias, Guid? parentKey)
        {
            var nodeName = oldNode.Trashed
                ? oldNode.Name.Truncate(250, "") // Truncate to make sure we don't have max length (255) issues with Umbraco appending '(n)' numbers to the name if a non trashed duplicate exists
                : oldNode.Name;

            var newNode = parentKey == null
                ? _contentService.Create(nodeName, -1, contentTypeAlias)
                : _contentService.Create(nodeName, (Guid)parentKey, contentTypeAlias);

            newNode.SortOrder = oldNode.SortOrder;

            // Save default Name or variant names
            var contentType = _contentTypeService.Get(contentTypeAlias);
            if (contentType?.VariesByCulture() == true)
            {
                if (oldNode.VariesByCulture)
                {
                    foreach (var variantName in oldNode.NodeNames)
                    {
                        if (variantName.Culture == "default") { continue; }
                        var vName = oldNode.Trashed
                            ? variantName.Name.Truncate(250, "") // See above
                            : variantName.Name;
                        newNode.SetCultureName(vName, variantName.Culture);
                    }
                }
                else
                {
                    var oldDefaultCulture = oldNode.AvailableCultures.FirstOrDefault();

                    if (oldDefaultCulture.IsNullOrWhiteSpace())
                    {
                        var newDefaultCulture = _localizationService.GetDefaultLanguageIsoCode();
                        newNode.SetCultureName(nodeName, newDefaultCulture);
                    }
                    else
                    {
                        newNode.SetCultureName(nodeName, oldDefaultCulture);
                    }
                }
            }

            // Set the default template
            if (contentType?.DefaultTemplate?.Id != null)
            {
                newNode.TemplateId = contentType.DefaultTemplate.Id;
            }

            return newNode;
        }
    }
}