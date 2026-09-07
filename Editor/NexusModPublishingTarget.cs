using System;
using System.Threading;
using System.Threading.Tasks;

namespace DaftAppleGames.Editor.ModPublisher
{
    public sealed class NexusModPublishingTarget : IModPublishingTarget
    {
        public ModHostingSite Site => ModHostingSite.Nexus;
        public string DisplayName => "Nexus Mods";

        public bool TryValidate(ModPublishingContext context, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(context.NexusApiKey))
            {
                error = "Enter your Nexus Mods personal API key.";
                return false;
            }

            NexusModsUploadOptions options = context.Entry.NexusMods;
            if (options == null || string.IsNullOrWhiteSpace(options.FileGroupId))
            {
                error = "Enter the Nexus file Group ID shown in the file's API Info dialog.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(context.Changelog) &&
                string.IsNullOrWhiteSpace(options.GameScopedModId))
            {
                error = "Enter the Nexus game-scoped mod ID from the mod page URL when providing a changelog.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(context.Changelog) &&
                string.IsNullOrWhiteSpace(options.GameDomain))
            {
                error = "Enter the Nexus game domain when providing a changelog.";
                return false;
            }

            return true;
        }

        public string Describe(ModPublishingContext context) =>
            $"Nexus Mods file group {context.Entry.NexusMods.FileGroupId}";

        public async Task<ModPublishingResult> PublishAsync(
            ModPublishingContext context,
            IProgress<UploadProgress> progress,
            CancellationToken cancellationToken)
        {
            using (NexusModsApiClient client = new NexusModsApiClient(context.NexusApiKey))
            {
                string versionId = await client.UploadNewVersionAsync(
                    context.Entry.NexusMods,
                    context.ArchivePath,
                    context.Version,
                    context.Changelog,
                    progress,
                    cancellationToken);
                return new ModPublishingResult(versionId, $"Nexus version ID: {versionId}");
            }
        }
    }
}
