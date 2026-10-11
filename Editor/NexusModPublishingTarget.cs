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
            if (options == null)
            {
                error = "Configure the Nexus Mods upload options.";
                return false;
            }

            bool needsModId = string.IsNullOrWhiteSpace(options.FileGroupId) ||
                              !string.IsNullOrWhiteSpace(context.Changelog);
            if (needsModId &&
                string.IsNullOrWhiteSpace(options.GameScopedModId))
            {
                error = "Enter the Nexus game-scoped mod ID from the mod page URL when creating a file or providing a changelog.";
                return false;
            }

            if (needsModId &&
                string.IsNullOrWhiteSpace(options.GameDomain))
            {
                error = "Enter the Nexus game domain when creating a file or providing a changelog.";
                return false;
            }

            return true;
        }

        public string Describe(ModPublishingContext context) =>
            string.IsNullOrWhiteSpace(context.Entry.NexusMods.FileGroupId)
                ? $"New Nexus Mods file on {context.Entry.NexusMods.GameDomain}/mods/{context.Entry.NexusMods.GameScopedModId}"
                : $"Nexus Mods file group {context.Entry.NexusMods.FileGroupId}";

        public async Task<ModPublishingResult> PublishAsync(
            ModPublishingContext context,
            IProgress<UploadProgress> progress,
            CancellationToken cancellationToken)
        {
            using (NexusModsApiClient client = new NexusModsApiClient(context.NexusApiKey))
            {
                bool creatingFile = string.IsNullOrWhiteSpace(context.Entry.NexusMods.FileGroupId);
                string identifier = await client.UploadNewVersionAsync(
                    context.Entry.NexusMods,
                    context.ArchivePath,
                    context.Version,
                    context.Changelog,
                    progress,
                    cancellationToken,
                    () => ModVersionSettings.Instance.SaveSettings());
                return new ModPublishingResult(identifier, creatingFile
                    ? $"Nexus file Group ID: {identifier} (saved to mod configuration)"
                    : $"Nexus version ID: {identifier}");
            }
        }
    }
}
