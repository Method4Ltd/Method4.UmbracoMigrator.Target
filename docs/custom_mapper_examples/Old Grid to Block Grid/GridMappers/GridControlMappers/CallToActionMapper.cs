using Method4.UmbracoMigrator.Target.Core.Helpers;
using Method4.UmbracoMigrator.Target.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using MySite.Models.UmbracoModels;

namespace MySite.Migration.GridMappers.GridControlMappers
{
    public class CallToActionMapper
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IKeyTransformer _keyTransformer;


        public CallToActionMapper(IContentTypeService contentTypeService, IKeyTransformer keyTransformer)
        {
            _contentTypeService = contentTypeService;
            _keyTransformer = keyTransformer;
        }

        public dynamic Map(JObject oldJson)
        {
            var contentType = _contentTypeService.Get(CallToAction.ModelTypeAlias);

            //// Convert Properties ////

            // Background Colour
            var newBackgroundColourValue = "";
            if (oldJson["selectedColor"]?["value"].ToString().IsNullOrWhiteSpace() == false)
            {
                newBackgroundColourValue = oldJson["selectedColor"].ToString();
            }

            // CTA Text
            var newTextValue = "";
            if (oldJson["text"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                newTextValue = oldJson["text"]!.ToString();
                if (_keyTransformer.TryTransformOldKeyReferences("Umbraco.TinyMCE", newTextValue, out var value))
                {
                    newTextValue = value;
                }
            }

            // Build Block
            dynamic newBlockObject = new ExpandoObject();
            newBlockObject.contentTypeKey = contentType!.Key.ToString();
            newBlockObject.udi = new GuidUdi("element", Guid.NewGuid()).ToString();

            newBlockObject.backgroundColour = newBackgroundColourValue;
            newBlockObject.text = newTextValue;

            return newBlockObject;
        }
    }
}
