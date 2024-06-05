namespace MySite.Migration.BlockMappers 
{
    public class ImageBlockMapper 
    {
        private IContentTypeSerivce _contentTypeService;

        public ImageBlockMapper(IContentTypeService contentTypeService) 
        {
            _contentTypeService = contentTypeService;
        }

        public ImageBlock CreateImageBlock(ImageMacro imageMacro, string udi) 
        {
            var contentTypes = _contentTypeService.GetAll();

            return new ImageBlock 
            {
                ContentTypeKey = contentTypes.FirstOrDefault(x => x.Alias == "imageBlock"),
                Udi = udi,
                Image = imageMacro.Image
            }
        }
    }
}