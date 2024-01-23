using Method4.UmbracoMigrator.Target.Core.Helpers;
using Method4.UmbracoMigrator.Target.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using MySite.Migration.Helpers;
using MySite.Models.UmbracoModels;

namespace MySite.Migration.GridMappers.GridControlMappers
{
    public class ButtonLinkMapper
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IKeyTransformer _keyTransformer;

        public ButtonLinkMapper(IContentTypeService contentTypeService, IKeyTransformer keyTransformer)
        {
            _contentTypeService = contentTypeService;
            _keyTransformer = keyTransformer;
        }

        public dynamic Map(JObject oldJson)
        {
            var contentType = _contentTypeService.Get(ButtonLink.ModelTypeAlias);

            //// Convert Properties ////

            // Link
            var newLinkValue = "";
            if (oldJson["link"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                var oldValueString = System.Net.WebUtility.HtmlDecode(oldJson["link"].ToString());
                newLinkValue = oldValueString;

                if (_keyTransformer.TryTransformOldKeyReferences("Umbraco.MultiUrlPicker", newLinkValue, out var value))
                {
                    newLinkValue = value;
                }
            }

            // Build Block
            dynamic newBlockObject = new ExpandoObject();
            newBlockObject.contentTypeKey = contentType!.Key.ToString();
            newBlockObject.udi = new GuidUdi("element", Guid.NewGuid()).ToString();
            newBlockObject.link = newLinkValue;

            return newBlockObject;
        }
    }
}