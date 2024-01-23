using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.Helpers
{
    public class PropertyEditorConverter : IPropertyEditorConverter
    {
        private readonly ILogger<PropertyEditorConverter> _logger;
        private readonly IDataTypeService _dataTypeService;

        public PropertyEditorConverter(ILogger<PropertyEditorConverter> logger, IDataTypeService dataTypeService)
        {
            _logger = logger;
            _dataTypeService = dataTypeService;
        }

        public bool CanConvert(string newPropertyEditorAlias, string oldPropertyEditorAlias)
        {
            return newPropertyEditorAlias is Constants.PropertyEditors.Aliases.MediaPicker3
                   && oldPropertyEditorAlias is Constants.PropertyEditors.Aliases.MediaPicker or Constants.PropertyEditors.Legacy.Aliases.MediaPicker2;
        }
        
        public bool CanConvert(IProperty newProperty, MigrationProperty oldProperty)
        {
            return CanConvert(newProperty.PropertyType.PropertyEditorAlias, oldProperty.PropertyEditorAlias);
        }

        public string? Convert(string newPropertyEditorAlias, string oldPropertyEditorAlias, string oldPropertyValue)
        {
            if (oldPropertyValue.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (newPropertyEditorAlias is Constants.PropertyEditors.Aliases.MediaPicker3 
                && oldPropertyEditorAlias is Constants.PropertyEditors.Aliases.MediaPicker or Constants.PropertyEditors.Legacy.Aliases.MediaPicker2)
            {
                return ConvertMediaPickerValueToMediaPicker3Value(oldPropertyValue);
            }

            throw new ArgumentException($"The conversion of '{oldPropertyEditorAlias}' to '{newPropertyEditorAlias}' is not supported");
        }

        public bool TryConvert(string newPropertyEditorAlias, string oldPropertyEditorAlias, string oldPropertyValue, out string? result)
        {
            try
            {
                result = Convert(newPropertyEditorAlias, oldPropertyEditorAlias, oldPropertyValue);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert  value '{oldPropertyValue}' from '{oldPropertyEditorAlias}' to '{newPropertyEditorAlias}'", oldPropertyValue, oldPropertyEditorAlias, newPropertyEditorAlias);
                result = "";
                return false;
            }
        }

        ////////// Conversion Methods //////////

        public bool TryConvertMediaPickerValueToMediaPicker3Value(string mediaPickerValue, out string mediaPicker3Value)
        {
            try
            {
                mediaPicker3Value = ConvertMediaPickerValueToMediaPicker3Value(mediaPickerValue);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert legacy MediaPicker value into the MediaPicker3 format");
                mediaPicker3Value = "";
                return false;
            }
        }

        public string ConvertMediaPickerValueToMediaPicker3Value(string mediaPickerValue)
        {
            // MediaPicker value format: "umb://media/5598b628b39045328bb5dab06089e9d7,umb://...,umb://..."
            // MediaPicker3 value format: "[{"key":"9f224bee-5a9d-4818-8376-656d08c050b4","mediaKey":"8ac2c7bc-0acb-488e-a4e6-24d9ea5bdff7"}]"
            var oldUDIs = mediaPickerValue.Split(',');
            var newValues = new List<object>();

            foreach (var oldUDI in oldUDIs)
            {
                var oldKeyString = oldUDI.Replace($"umb://media/", "");
                var parseResult = Guid.TryParse(oldKeyString, out Guid oldKey);
                if (parseResult == false)
                {
                    _logger.LogWarning("Failed to parse Key from UDI: {oldUDI}", oldUDI);
                    throw new Exception($"Failed to parse Key from UDI: {oldUDI}");
                }

                newValues.Add(new
                {
                    key = Guid.NewGuid().ToString(),
                    mediaKey = oldKey.ToString(),
                });
            }

            return JsonSerializer.Serialize(newValues);
        }
    }
}
