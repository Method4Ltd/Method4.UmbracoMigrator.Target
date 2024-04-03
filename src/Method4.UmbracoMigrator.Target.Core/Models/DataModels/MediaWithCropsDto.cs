using System.Text.Json.Serialization;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;

namespace Method4.UmbracoMigrator.Target.Core.Models.DataModels
{
    public class MediaWithCropsDto
    {
        [JsonPropertyName("key")]
        public Guid Key { get; set; }

        [JsonPropertyName("mediaKey")]
        public Guid MediaKey { get; set; }

        [JsonPropertyName("crops")]
        public IEnumerable<ImageCropperValue.ImageCropperCrop>? Crops { get; set; }

        [JsonPropertyName("focalPoint")]
        public ImageCropperValue.ImageCropperFocalPoint? FocalPoint { get; set; }

    }
}