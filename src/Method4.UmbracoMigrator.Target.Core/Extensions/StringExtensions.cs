namespace Method4.UmbracoMigrator.Target.Core.Extensions
{
    internal static class StringExtensions
    {
        public static string? Truncate(this string? value, int maxLength = 50, string truncationSuffix = "…")
        {
            return value?.Length > maxLength
                ? value.Substring(0, maxLength) + truncationSuffix
                : value;
        }
    }
}