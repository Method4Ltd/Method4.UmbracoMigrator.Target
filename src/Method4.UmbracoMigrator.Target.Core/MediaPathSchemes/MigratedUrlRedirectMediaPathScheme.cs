using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Skybrud.Umbraco.Redirects.Models;
using Skybrud.Umbraco.Redirects.Models.Options;
using Skybrud.Umbraco.Redirects.Services;
using System.Text.RegularExpressions;
using Method4.UmbracoMigrator.Target.Core.Options;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.IO.MediaPathSchemes;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Method4.UmbracoMigrator.Target.Core.MediaPathSchemes
{
    /// <summary>
    /// Adds redirects if the media path slug changes on a file change
    /// </summary>
    public class MigratedUrlRedirectMediaPathScheme : UniqueMediaPathScheme, IMediaPathScheme
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<MigratorTargetSettings> _settings;

        public MigratedUrlRedirectMediaPathScheme(IServiceProvider serviceProvider, IOptions<MigratorTargetSettings> settings) : base()
        {
            _serviceProvider = serviceProvider;
            _settings = settings;
        }

        public new string GetFilePath(MediaFileManager fileManager, Guid itemGuid, Guid propertyGuid, string filename)
        {
            var baseResult = base.GetFilePath(fileManager, itemGuid, propertyGuid, filename);

            // For testing
            //return $"oldMigratedSlug/{filename}";

            if (_settings.Value.EnableMediaRedirectGeneration == false)
            {
                return baseResult;
            }

            var fileUploadPropertyTypeAlias = "umbracoFile";
            if (_settings.Value.MediaFileUploadPropertyAlias.IsNullOrWhiteSpace() == false)
            {
                fileUploadPropertyTypeAlias = _settings.Value.MediaFileUploadPropertyAlias;
            }

            // Get the media service (can't inject this as that would cause a circular dependency)
            var scope = _serviceProvider.CreateScope();
            var mediaService = scope.ServiceProvider.GetService<IMediaService>();
            if (mediaService == null)
            {
                return baseResult;
            }

            // Get the media node from the db
            var mediaItem = mediaService.GetById(itemGuid);
            if (mediaItem == null)
            {
                return baseResult;
            }

            // Get the old file path
            var oldFilePath = mediaItem.GetValue<string>(fileUploadPropertyTypeAlias);
            if (oldFilePath.IsNullOrWhiteSpace())
            {
                return baseResult;
            }

            // Get the old slug
            var oldSlug = "";
            var oldFilename = "";
            var newSlug = baseResult.Split('/')[0];
            var newFilename = baseResult.Split('/')[1];

            string pattern = @"/media/([a-zA-Z0-9]+)/";
            Match match = Regex.Match(oldFilePath, pattern);

            if (match.Success)
            {
                oldSlug = match.Groups[1].Value;
                oldFilename = oldFilePath.Replace($"/media/{oldSlug}/", "");
            }

            // If the old slug matches the new slug, we don't care
            if (oldSlug == newSlug)
            {
                return baseResult;
            }

            // If the filename is different, we don't care
            if (oldFilename != newFilename)
            {
                return baseResult;
            }

            // Add a redirect to the new slug url
            var redirectsService = scope.ServiceProvider.GetService<IRedirectsService>();
            if (redirectsService != null)
            {
                var redirect = new AddRedirectOptions()
                {
                    Type = RedirectType.Permanent,
                    OriginalUrl = $"{oldFilePath}",
                    Destination = new RedirectDestination()
                    {
                        Type = RedirectDestinationType.Media,
                        Key = mediaItem.Key,
                        Id = mediaItem.Id,
                        Url = $"/media/{baseResult}"
                    },
                    
                };
                redirectsService.AddRedirect(redirect);
            }

            return baseResult;
        }
    }
}