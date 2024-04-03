using Method4.UmbracoMigrator.Target.Core.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using Method4.UmbracoMigrator.Target.Core.Models.DataModels;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.Helpers
{
    internal class KeyTransformer : IKeyTransformer
    {
        private readonly IRelationLookupService _relationLookupService;
        private readonly ILogger<KeyTransformer> _logger;

        public KeyTransformer(IRelationLookupService relationLookupService, ILogger<KeyTransformer> logger)
        {
            _relationLookupService = relationLookupService;
            _logger = logger;
        }

        public bool CanHaveKeyTransformed(string propertyEditorAlias)
        {
            switch (propertyEditorAlias)
            {
                case Constants.PropertyEditors.Aliases.MediaPicker:
                case Constants.PropertyEditors.Legacy.Aliases.MediaPicker2:
                case Constants.PropertyEditors.Aliases.MediaPicker3:
                case Constants.PropertyEditors.Aliases.ContentPicker:
                case Constants.PropertyEditors.Aliases.MultiUrlPicker:
                case Constants.PropertyEditors.Aliases.MultiNodeTreePicker:
                case Constants.PropertyEditors.Aliases.TinyMce:
                    return true;

                default:
                    return false;
            }
        }

        public string TransformOldKeyReferences(string propertyEditorAlias, string value)
        {
            switch (propertyEditorAlias)
            {
                case Constants.PropertyEditors.Aliases.MediaPicker:
                case Constants.PropertyEditors.Legacy.Aliases.MediaPicker2:
                    value = TransformMediaPicker(value);
                    break;

                case Constants.PropertyEditors.Aliases.MediaPicker3:
                    value = TransformMediaPicker3(value);
                    break;

                case Constants.PropertyEditors.Aliases.ContentPicker:
                    value = TransformContentPicker(value);
                    break;

                case Constants.PropertyEditors.Aliases.MultiUrlPicker:
                    value = TransformMultiUrlPicker(value);
                    break;

                case Constants.PropertyEditors.Aliases.MultiNodeTreePicker:
                    value = TransformMultiNodeTreePicker(value);
                    break;

                case Constants.PropertyEditors.Aliases.TinyMce:
                    value = TransformRichText(value);
                    break;

                default:
                    throw new ArgumentException($"The property editor '{propertyEditorAlias}' is currently not supported");
            }

            return value;
        }

        public bool TryTransformOldKeyReferences(string propertyEditorAlias, string value, out string transformedValue)
        {
            try
            {
                transformedValue = TransformOldKeyReferences(propertyEditorAlias, value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transform old key references in value: [{value}]", value);
                transformedValue = "";
                return false;
            }
        }

        private string TransformMediaPicker(string oldValue) // Old value format: "umb://media/5598b628b39045328bb5dab06089e9d7,umb://...,umb://..."
        {
            var newUdiList = TransformCommaSeparatedUdiList(oldValue);
            return newUdiList == null
                ? oldValue
                : string.Join(',', newUdiList);
        }

        private string TransformMediaPicker3(string oldValue) // Old value format: "{"key":"9f224bee-5a9d-4818-8376-656d08c050b4","mediaKey":"8ac2c7bc-0acb-488e-a4e6-24d9ea5bdff7"}"
        {
            var newPickedMedia = new List<dynamic>();
            var oldPickedMedias = JsonSerializer.Deserialize<List<MediaWithCropsDto>>(oldValue) ?? new List<MediaWithCropsDto>();

            foreach (var oldPickedMedia in oldPickedMedias)
            {
                var oldKey = oldPickedMedia.MediaKey;
                var relation = _relationLookupService.GetRelationByOldKey(oldKey);
                if (relation == null)
                {
                    _logger.LogError("Could not find key lookup for {oldKey}", oldKey);
                    return oldValue;
                }

                if (oldPickedMedia.Crops?.Any() == true)
                {
                    newPickedMedia.Add(new
                    {
                        key = Guid.NewGuid().ToString(),
                        mediaKey = relation.NewKeyAsString,
                        crops = oldPickedMedia.Crops,
                        focalPoint = oldPickedMedia.FocalPoint
                    });
                }
                else
                {
                    newPickedMedia.Add(new
                    {
                        key = Guid.NewGuid().ToString(),
                        mediaKey = relation.NewKeyAsString,
                    });
                }
            }

            return JsonSerializer.Serialize(newPickedMedia);
        }

        private string TransformContentPicker(string oldValue) // Old value format: "umb://document/5598b628b39045328bb5dab06089e9d7,umb://...,umb://..."
        {
            var newUdiList = TransformCommaSeparatedUdiList(oldValue);
            return newUdiList == null
                ? oldValue
                : string.Join(',', newUdiList);
        }

        private string TransformMultiNodeTreePicker(string oldValue) // Old value format: "umb://document/5598b628b39045328bb5dab06089e9d7,umb://media/...,umb://..."
        {
            var newUdiList = TransformCommaSeparatedUdiList(oldValue);
            return newUdiList == null
                ? oldValue
                : string.Join(',', newUdiList);
        }

        private string TransformMultiUrlPicker(string oldValue) // Old value format: "{"name":"Community","udi":"umb://document/2cb4c092564a42c69b18bd31e117004e"},{"name":"Google","url":"https://www.google.co.uk/"},{"name":"Community front row","udi":"umb://media/167ee71553ff4a8bab507d630c8448fa"}"
        {
            // {"Name":"Download","Target":null,"Type":2,"Udi":null,"Url":" /DocumentLibrary/A_PDF.pdf"}

            var newPickedUrls = new List<dynamic>();
            var oldPickedUrls = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(oldValue) ?? new List<Dictionary<string, object?>>();

            foreach (var oldPickedUrl in oldPickedUrls)
            {
                var isNode = oldPickedUrl.TryGetValue("udi", out var oldUdi);
                var hasName = oldPickedUrl.TryGetValue("name", out var oldName);
                var hasTarget = oldPickedUrl.TryGetValue("target", out var oldtarget);

                if (oldUdi == null || isNode == false)
                {
                    newPickedUrls.Add(oldPickedUrl);
                    continue;
                }

                var isMedia = oldUdi.ToString()?.StartsWith("umb://media/") == true;
                string oldKeyString = oldUdi.ToString()?.Replace(isMedia ? "umb://media/" : "umb://document/", "") ?? "";

                if (oldKeyString.IsNullOrWhiteSpace())
                {
                    newPickedUrls.Add(oldPickedUrl);
                    continue;
                }

                var parseResult = Guid.TryParse(oldKeyString, out Guid oldKey);
                if (parseResult == false)
                {
                    _logger.LogWarning("Failed to parse Key in MultiUrlPicker value: {oldUdi}", oldUdi);
                    newPickedUrls.Add(oldPickedUrl);
                    continue;
                }

                var relation = _relationLookupService.GetRelationByOldKey(oldKey);
                var newKey = relation?.NewKeyAsGuid;
                if (newKey == null)
                {
                    _logger.LogError("Could not find key lookup for {oldKey}", oldKey);
                    newPickedUrls.Add(oldPickedUrl);
                    continue;
                }

                newPickedUrls.Add(new
                {
                    name = hasName ? oldName : "",
                    target = hasTarget ? oldtarget : null,
                    udi = isMedia
                        ? $"umb://media/{newKey!.ToString()!.Replace("-", "")}"
                        : $"umb://document/{newKey!.ToString()!.Replace("-", "")}",
                });
            }

            return JsonSerializer.Serialize(newPickedUrls);
        }

        private List<string>? TransformCommaSeparatedUdiList(string oldValue)
        {
            var newUdiList = new List<string>();
            var oldUdiList = oldValue.Split(',');

            foreach (var oldUdi in oldUdiList)
            {
                var nodeType = oldUdi.StartsWith("umb://media/") ? "media" : "document";
                var parseResult = UdiToGuid(oldUdi, out Guid oldKey, nodeType);
                if (parseResult == false) { continue; }

                var relation = _relationLookupService.GetRelationByOldKey(oldKey);
                var newKey = relation?.NewKeyAsGuid;
                if (newKey == null)
                {
                    _logger.LogError("Could not find key lookup for {oldKey}", oldKey);
                    continue;
                }

                newUdiList.Add($"umb://{nodeType}/{newKey!.ToString()!.Replace("-", "")}");
            }

            return newUdiList.Any()
                ? newUdiList
                : null;
        }

        private string TransformRichText(string oldValue)
        {
            var newValue = oldValue;
            var foundUdiList = Regex.Matches(oldValue, @"umb://(media|document)/(.{32})", RegexOptions.Multiline).ToList();

            foreach (var udi in foundUdiList)
            {
                var oldUdi = udi.Value.ToString();
                if (oldUdi.IsNullOrWhiteSpace()) { continue; }

                // Get new Key
                var nodeType = oldUdi.StartsWith("umb://media/") ? "media" : "document";
                var parseResult = UdiToGuid(oldUdi, out Guid oldKey, nodeType);
                if (parseResult == false) { continue; }

                var relation = _relationLookupService.GetRelationByOldKey(oldKey);
                var newKey = relation?.NewKeyAsGuid;
                if (newKey == null)
                {
                    _logger.LogError("Could not find key lookup for {oldKey}", oldKey);
                    continue;
                }

                // Replace old UDI
                var newUdi = $"umb://{nodeType}/{newKey!.ToString()!.Replace("-", "")}";
                newValue = newValue.Replace(oldUdi, newUdi);
            }

            return newValue;
        }

        private bool UdiToGuid(string udi, out Guid guid, string nodeType = "document")
        {
            var oldKeyString = udi.Replace($"umb://{nodeType}/", "");
            var parseResult = Guid.TryParse(oldKeyString, out guid);
            if (parseResult == false)
            {
                _logger.LogError("Failed to parse Key from UDI: {udi}", udi);
                return false;
            }
            return true;
        }
    }
}