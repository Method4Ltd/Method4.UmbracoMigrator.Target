using System.Text.RegularExpressions;

namespace MySite.Migration.Helpers 
{
    public static class MapRichTextWithMacrosHelper 
    {
        private readonly IKeyTransformer _keyTransformer;
        private readonly IGridComponentMapper _gridComponentMapper;
        private readonly IContentTypeService _contentTypeService;

        public MapRichTextWithMacrosHelper(
            IKeyTransformer keyTransformer, 
            IGridComponentMapper gridComponentMapper,
            IContentTypeService contentTypeService
            ) 
        {
            _keyTransformer = keyTransformer;
            _gridComponentMapper = gridComponentMapper;
            _contentTypeService = contentTypeService;
        }

        public string MapRichText(string oldRichText) 
        {
            var newRichText = new RichText();

            var oldRichTextReferencesTransformed = _keyTransformer.TransformOldKeyReferences("Umbraco.TinyMCE", oldRichText);

            MatchCollection macros = Regex.Matches(oldRichTextReferencesTransformed, @"<\?UMBRACO_MACRO.*/>");

            foreach (Match m in macros) 
            {
                var macroString = m.Value;
                var guidUdi = new GuidUdi("element", Guid.NewGuid());

                var macroAlias = GetPropertyValue(macroString, "macroAlias");
                if (!macroAlias.IsNullOrWhiteSpace())
                {
                    string markupBlockText = "<umb-rte-block data-content-udi=\"{blockElementUdi}\"><!--Umbraco-Block--></umb-rte-block>";
                    markupBlockText = markupBlockText.Replace("{blockElementUdi}", guidUdi.ToString());
                    oldRichTextReferencesTransformed = oldRichTextReferencesTransformed.Replace(macroString, markupBlockText);
                    newRichText.Blocks.Layout.ContentUdis.Add(new Dictionary<string, string>
                    {
                        {"contentUdi", guidUdi.ToString() },
                    });
                    newRichText.Blocks.ContentData.Add(GetBlock(macroAlias, macroString, guidUdi.ToString()));
                }
            }
        }

        private object GetBlock(string macroAlias, string macroString, string udi) 
        {
            switch (macroAlias) 
            {
                case "image":
                    return CreateImageBlock(macroString, udi);

                default:
                    return null;

            }
        }

        private ImageBlock CreateImageBlock(string macroString, string udi) 
        {
            /// For this example the image field is a udi that has already been transformed
            /// using the key transformer above. Alternatively you could use an unaltered
            /// string of the rich text and use the relation service to find the new media
            /// item from the old key founf in the old udi
            var image = GetPropertyValue(macroString, "image");
            var contentTypes = _contentTypeService.GetAll();

            return new ImageBlock 
            { 
                ContentTypeKey = contentTypes.FirstOrDefault(x => x.Alias == "imageBlock").Key.ToString(),
                Udi = udi,
                Image = image
            }
        }

        private string GetPropertyValue(string richTextString, string propertyName)
        {
            var match = Regex.Match(richTextString, propertyName + @"="".+?""");
            if (match.Success)
            {
                return match.Value.Split('=').Last().Replace("\"", "");
            }
            return "";
        }
    }
}