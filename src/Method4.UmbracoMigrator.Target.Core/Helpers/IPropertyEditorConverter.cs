using Method4.UmbracoMigrator.Target.Core.Models.MigrationModels;
using Umbraco.Cms.Core.Models;

namespace Method4.UmbracoMigrator.Target.Core.Helpers
{
    public interface IPropertyEditorConverter
    {
        /// <summary>
        /// Checks wether the given old property can be converted into the new property's format.
        /// </summary>
        /// <param name="newPropertyEditorAlias"></param>
        /// <param name="oldPropertyEditorAlias"></param>
        /// <returns></returns>
        bool CanConvert(string newPropertyEditorAlias, string oldPropertyEditorAlias);

        /// <summary>
        /// Checks wether the given old property can be converted into the new property's format.
        /// </summary>
        /// <param name="newProperty"></param>
        /// <param name="oldProperty"></param>
        /// <returns></returns>
        bool CanConvert(IProperty newProperty, MigrationProperty oldProperty);

        /// <summary>
        /// Automatically selects and runs the appropreate conversion method based on the PropertyEditorAlias of the given properties
        /// </summary>
        /// <param name="newPropertyEditorAlias"></param>
        /// <param name="oldPropertyEditorAlias"></param>
        /// <param name="oldPropertyValue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the given property's property editor is not supported
        /// </exception>
        string? Convert(string newPropertyEditorAlias, string oldPropertyEditorAlias, string oldPropertyValue);

        /// <summary>
        /// Automatically selects and runs the appropreate conversion method based on the PropertyEditorAlias of the given properties
        /// </summary>
        /// <param name="newPropertyEditorAlias"></param>
        /// <param name="oldPropertyEditorAlias"></param>
        /// <param name="oldPropertyValue"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryConvert(string newPropertyEditorAlias, string oldPropertyEditorAlias, string oldPropertyValue, out string? result);

        ////////// Conversion Methods //////////

        /// <summary>
        /// Attempts to convert the given value from the legacy MediaPicker format, into the new MediaPicker3 format
        /// </summary>
        /// <param name="mediaPickerValue"></param>
        /// <returns></returns>
        string ConvertMediaPickerValueToMediaPicker3Value(string mediaPickerValue);

        /// <summary>
        /// Attempts to convert the given value from the legacy MediaPicker format, into the new MediaPicker3 format
        /// </summary>
        /// <param name="mediaPickerValue"></param>
        /// <param name="mediaPicker3Value"></param>
        /// <returns></returns>
        bool TryConvertMediaPickerValueToMediaPicker3Value(string mediaPickerValue, out string mediaPicker3Value);
    }
}