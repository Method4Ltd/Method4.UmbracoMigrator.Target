using Humanizer;
using System.Diagnostics;

namespace Method4.UmbracoMigrator.Target.Core.Extensions
{
    internal static class StopwatchExtensions
    {
        public static string Humanize(this Stopwatch stopwatch)
        {
            return stopwatch.Elapsed.Humanize(4);
        }
    }
}