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
    public class ArticlesHomeMapper
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IContentService _contentService;
        private readonly IKeyTransformer _keyTransformer;
        private readonly IIdLookupService _idLookupService;


        public ArticlesHomeMapper(IContentTypeService contentTypeService,
            IContentService contentService,
            IKeyTransformer keyTransformer,
            IIdLookupService idLookupService)
        {
            _contentTypeService = contentTypeService;
            _contentService = contentService;
            _keyTransformer = keyTransformer;
            _idLookupService = idLookupService;
        }

        public dynamic Map(JObject oldJson)
        {
            var contentType = _contentTypeService.Get(ArticlesHome.ModelTypeAlias);

            //// Convert Properties ////

            // Articles
            var newArticlesValue = "";
            if (oldJson["articles"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                // For some reason the old MultiNodeTreepicker on the ArticlesHome macro stored it's values as a comma separated list of IDs.
                // therefore we need to convert that into a comma seperated list of UDIs
                var oldIds = oldJson["articles"].ToString().Split(',').ToList();
                var newUdiList = FormatHelper.OldIdListToNewUdiList(oldIds, _idLookupService, _contentService);
                newArticlesValue = string.Join(",", newUdiList);
            }

            // Link
            var newLinkValue = "";
            if (oldJson["link"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                newLinkValue = oldJson["link"].ToString();
                if (_keyTransformer.TryTransformOldKeyReferences("Umbraco.ContentPicker", newLinkValue, out var value))
                {
                    newLinkValue = value;
                }
            }

            // Link Placeholder Text
            var newLinkTextValue = "";
            if (oldJson["linkPlaceholderText"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                newLinkTextValue = oldJson["linkPlaceholderText"]!.ToString();
            }

            // Build Block
            dynamic newBlockObject = new ExpandoObject();
            newBlockObject.contentTypeKey = contentType!.Key.ToString();
            newBlockObject.udi = new GuidUdi("element", Guid.NewGuid()).ToString();

            newBlockObject.articles = newArticlesValue;
            newBlockObject.link = newLinkValue;
            newBlockObject.linkPlaceholderText = newLinkTextValue;

            return newBlockObject;
        }
    }
}
