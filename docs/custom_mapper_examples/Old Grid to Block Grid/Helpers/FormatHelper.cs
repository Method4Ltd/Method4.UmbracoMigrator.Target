using Method4.UmbracoMigrator.Target.Core.Services;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace MySite.Migration.Helpers
{
    public static class FormatHelper
    {
        public static bool TryConvertStringToBoolean(string str, out bool result)
        {
            var BooleanStringOff = new[] { "0", "off", "no" };
            var BooleanStringOn = new[] { "1", "on", "yes" };

            if (string.IsNullOrEmpty(str))
            {
                result = false;
                return false;
            }
            else if (BooleanStringOff.Contains(str, StringComparer.InvariantCultureIgnoreCase))
            {
                result = false;
                return true;
            }
            else if (BooleanStringOn.Contains(str, StringComparer.InvariantCultureIgnoreCase))
            {
                result = true;
                return true;
            }

            if (bool.TryParse(str, out result) == false) {
                return false;
            }

            return true;
        }

        public static List<string> OldIdListToNewUdiList(List<string> oldIds, IIdLookupService idLookupService, IContentService contentService)
        {
            var udiList = new List<string>();
            foreach (var oldId in oldIds)
            {
                var newId = idLookupService.GetNewId(oldId);
                if (newId.IsNullOrWhiteSpace()) { continue; }

                var newNode = contentService.GetById(int.Parse(newId));
                if (newNode == null) { continue; }

                udiList.Add($"umb://document/{newNode.Key!.ToString()!.Replace("-", "")}");
            }
            return udiList;
        }
    }
}