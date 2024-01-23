using Umbraco.Cms.Core.Models;

namespace Method4.UmbracoMigrator.Target.Core.Helpers
{
    public interface IKeyTransformer
    {
        /// <summary>
        /// Checks wether the given property alias is supported and can be transformed.
        /// Supported Property Editors: Umbraco.MediaPicker, Umbraco.MediaPicker3, Umbraco.ContentPicker, Umbraco.MultiUrlPicker, Umbraco.MultiNodeTreePicker, Umbraco.TinyMCE.
        /// </summary>
        /// <param name="propertyEditorAlias"></param>
        /// <returns></returns>
        public bool CanHaveKeyTransformed(string propertyEditorAlias);

        /// <summary>
        /// Transformes the old Key (guid) references, in the given value, into their corresponding new Keys.
        /// Supported Property Editors: Umbraco.MediaPicker, Umbraco.MediaPicker3, Umbraco.ContentPicker, Umbraco.MultiUrlPicker, Umbraco.MultiNodeTreePicker, Umbraco.TinyMCE.
        /// </summary>
        /// <param name="propertyEditorAlias"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException">
        /// Thrown when the given property's property editor is not supported
        /// </exception>
        /// <returns></returns>
        public string TransformOldKeyReferences(string propertyEditorAlias, string value);

        /// <summary>
        /// Transformes the old Key (guid) references, in the given value, into their corresponding new Keys.
        /// Supported Property Editors: Umbraco.MediaPicker, Umbraco.MediaPicker3, Umbraco.ContentPicker, Umbraco.MultiUrlPicker, Umbraco.MultiNodeTreePicker, Umbraco.TinyMCE.
        /// </summary>
        /// <param name="propertyEditorAlias"></param>
        /// <param name="value"></param>
        /// <param name="transformedValue"></param>
        /// <returns></returns>
        public bool TryTransformOldKeyReferences(string propertyEditorAlias, string value, out string transformedValue);
    }
}