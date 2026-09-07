using System;

namespace DaftAppleGames.Editor.ModPublisher
{
    public static class ReleaseTemplate
    {
        public const string DefaultTagName = "{archiveName}-v{version}";
        public const string DefaultReleaseTitle = "{displayName} v{version}";

        public static string Expand(string template, ModVersionEntry entry, string version)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            string archiveName = entry.Manifest == null || entry.Manifest.Identity == null ||
                                 string.IsNullOrWhiteSpace(entry.Manifest.Identity.Name)
                ? entry.Name
                : entry.Manifest.Identity.Name;
            return Expand(template, entry.Name, archiveName, version);
        }

        public static string Expand(
            string template,
            string displayName,
            string archiveName,
            string version)
        {
            return (template ?? string.Empty)
                .Replace("{displayName}", displayName ?? string.Empty)
                .Replace("{archiveName}", archiveName ?? string.Empty)
                .Replace("{version}", version ?? string.Empty);
        }
    }
}
