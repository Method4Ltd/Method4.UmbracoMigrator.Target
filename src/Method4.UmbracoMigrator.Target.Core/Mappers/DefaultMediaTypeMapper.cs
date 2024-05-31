using Method4.UmbracoMigrator.Target.Core.Helpers;
using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.Mappers
{
    public class DefaultMediaTypeMapper : IInternalMediaTypeMapping
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IMediaService _mediaService;
        private readonly IKeyTransformer _keyTransformer;
        private readonly IPropertyEditorConverter _propertyEditorConverter;
        private readonly ILogger<DefaultMediaTypeMapper> _logger;

        public DefaultMediaTypeMapper(IContentTypeService contentTypeService, IMediaService mediaService, IKeyTransformer keyTransformer, IPropertyEditorConverter propertyEditorConverter, ILogger<DefaultMediaTypeMapper> logger)
        {
            _contentTypeService = contentTypeService;
            _mediaService = mediaService;
            _keyTransformer = keyTransformer;
            _propertyEditorConverter = propertyEditorConverter;
            _logger = logger;
        }

        public bool CanIMap(MigrationMedia migrationNode)
        {
            // Look for a Content Type that has the same alias/name as the old one
            var contentTypeAliases = _contentTypeService.GetAllContentTypeAliases();
            var contentTypeMatch = contentTypeAliases.Contains(migrationNode.ContentType);
            return contentTypeMatch;
        }

        public IMedia CreateNode(MigrationMedia oldNode, string contentTypeAlias, Guid parentKey)
        {
            var newNode = _mediaService.CreateMedia(oldNode.Name, parentKey, contentTypeAlias);
            newNode.SortOrder = oldNode.SortOrder;
            return newNode;
        }

        public IMedia CreateRootNode(MigrationMedia oldNode, string contentTypeAlias)
        {
            var newNode = _mediaService.CreateMedia(oldNode.Name, -1, contentTypeAlias);
            newNode.SortOrder = oldNode.SortOrder;
            return newNode;
        }

        public IMedia MapNode(MigrationMedia oldNode, IMedia newNode, bool overwriteExistingValues)
        {
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

                // Media cannot vary by culture, every value will be "default"
                var value = oldProperty.GetDefaultValue;

                // if not null, lets do some things
                if (value.IsNullOrWhiteSpace() == false)
                {
                    // Update Key references, if we can
                    if (_keyTransformer.CanHaveKeyTransformed(oldProperty.PropertyEditorAlias))
                    {
                        value = _keyTransformer.TransformOldKeyReferences(oldProperty.PropertyEditorAlias, value!);
                    }

                    // Convert to new format, if we can
                    if (_propertyEditorConverter.CanConvert(newProperty, oldProperty))
                    {
                        value = _propertyEditorConverter.Convert(newProperty.PropertyType.PropertyEditorAlias, oldProperty.PropertyEditorAlias, value);
                    }
                }

                try
                {
                    // Save the value
                    newNode.SetValue(oldProperty.Alias, value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save property {propertyAlias} on node {nodeId} [{nodeKey}]. Value: {value}", oldProperty.Alias, newNode.Id, newNode.Key, value);
                    throw;
                }
            }

            // TODO: Support variant properties
            return newNode;
        }
    }
}