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
    public class HeroBannerMapper
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IMediaService _mediaService;
        private readonly IIdLookupService _idLookupService;

        public HeroBannerMapper(IContentTypeService contentTypeService,
            IMediaService mediaService,
            IIdLookupService idLookupService)
        {
            _contentTypeService = contentTypeService;
            _mediaService = mediaService;
            _idLookupService = idLookupService;
        }

        public dynamic Map(JObject oldJson)
        {
            var contentType = _contentTypeService.Get(HeroBanner.ModelTypeAlias);

            //// Convert Properties ////

            // Banner Image
            var newImageValue = "";
            if (oldJson["imageId"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                // old value is just the ID, so we need to build the media picker value manually
                var newImageIdString = _idLookupService.GetNewId(oldJson["imageId"]!.ToString());
                if (int.TryParse(newImageIdString, out var newImageId))
                {
                    var newImage = _mediaService.GetById(newImageId);
                    if (newImage != null)
                    {
                        newImageValue = JsonConvert.SerializeObject(new List<dynamic>
                        {
                            new
                            {
                                key = Guid.NewGuid().ToString(),
                                mediaKey = newImage.Key.ToString(),
                            }
                        });
                    }
                }
            }

            // Image Alt Text
            var newImageAltTextValue = "";
            if (oldJson["imageAltText"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                newImageAltTextValue = oldJson["imageAltText"]!.ToString();
            }

            // Header Text
            var newHeaderTextValue = "";
            if (oldJson["headerText"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                newHeaderTextValue = oldJson["headerText"]!.ToString();
            }

            // Set as H1
            var newSetHeaderTextAsH1Value = false;
            if (oldJson["setHeaderTextAsH1"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                if (bool.TryParse(oldJson["setHeaderTextAsH1"]!.ToString(), out var oldValue))
                {
                    newSetHeaderTextAsH1Value = oldValue;
                }
            }

            // Sub Header Text
            var newSubHeaderTextValue = "";
            if (oldJson["subHeaderText"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                newSubHeaderTextValue = oldJson["subHeaderText"]!.ToString();
            }

            // Content on Left
            var newContentOnLeftValue = false;
            if (oldJson["setContentPlacement"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                if (bool.TryParse(oldJson["setContentPlacement"]!.ToString(), out var oldValue))
                {
                    newContentOnLeftValue = oldValue;
                }
            }

            // Content
            var newContentValue = "";
            if (oldJson["content"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                newContentValue = oldJson["content"]!.ToString();
            }

            // Link & Link Text
            var newLinkValue = "";
            if (oldJson["link"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                // old value is just a url, so we need to build the url picker value manually
                newLinkValue = JsonConvert.SerializeObject(new List<dynamic>
                {
                    new
                    {
                        Name = oldJson["linkText"]!.ToString() ?? "",
                        Target = "_self",
                        Url = oldJson["link"]?.ToString()
                    }
                });
            }

            // Second Link & Second Link Text
            var newSecondLinkValue = "";
            if (oldJson["secondLink"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                // old value is just a url, so we need to build the url picker value manually
                newSecondLinkValue = JsonConvert.SerializeObject(new List<dynamic>
                {
                   new
                   {
                       Name = oldJson["secondLinkText"]!.ToString() ?? "",
                       Target = "_self",
                       Url = oldJson["secondLink"]?.ToString()
                   }
                });
            }

            // Additional Content
            var newAdditionalContentValue = "";
            if (oldJson["additionalContent"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                newAdditionalContentValue = oldJson["additionalContent"]!.ToString();
            }

            // Additional Link & Additional Link Text
            var newAdditionalLinkValue = "";
            if (oldJson["additionalLink"]?.ToString().IsNullOrWhiteSpace() == false)
            {
                // old value is just a url, so we need to build the url picker value manually
                newAdditionalLinkValue = JsonConvert.SerializeObject(new List<dynamic>
                {
                    new
                    {
                        Name = oldJson["additionalLinkText"]!.ToString() ?? "",
                        Target = "_self",
                        Url = oldJson["additionalLink"]?.ToString()
                    }
                });
            }


            // Build Block
            dynamic newBlockObject = new ExpandoObject();
            newBlockObject.contentTypeKey = contentType!.Key.ToString();
            newBlockObject.udi = new GuidUdi("element", Guid.NewGuid()).ToString();

            newBlockObject.image = newImageValue;
            newBlockObject.imageAlternativeText = newImageAltTextValue;
            newBlockObject.headerText = newHeaderTextValue;
            newBlockObject.setAsH1 = newSetHeaderTextAsH1Value;
            newBlockObject.subHeaderText = newSubHeaderTextValue;
            newBlockObject.contentOnLeft = newContentOnLeftValue;
            newBlockObject.content = newContentValue;
            newBlockObject.link = newLinkValue;
            newBlockObject.secondLink = newSecondLinkValue;
            newBlockObject.additionalContent = newAdditionalContentValue;
            newBlockObject.additionalLink = newAdditionalLinkValue;

            return newBlockObject;
        }
    }
}
